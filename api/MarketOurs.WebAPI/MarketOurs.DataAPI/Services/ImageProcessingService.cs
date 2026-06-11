using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;

namespace MarketOurs.DataAPI.Services;

/// <summary>
/// 图片处理服务 — 将 GIF 转为 Animated WebP，减小文件体积。
/// 非 GIF 文件原样返回（不做处理）。
/// </summary>
public class ImageProcessingService(ILogger<ImageProcessingService> logger)
{
    /// <summary>
    /// WebP 编码质量 (0-100)。默认 80，可被环境变量 <c>WEBP_QUALITY</c> 覆盖。
    /// </summary>
    private readonly int _webpQuality = ParseQualityEnv("WEBP_QUALITY", 80);

    /// <summary>
    /// 处理上传的图片文件。
    /// 如果文件是 GIF，则转为 Animated WebP；否则返回 null 表示无需处理。
    /// 转换失败时返回 null（降级为原始文件上传），不抛异常。
    /// </summary>
    public async Task<IFormFile?> ProcessAsync(IFormFile file)
    {
        var sw = Stopwatch.StartNew();

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not ".gif")
        {
            logger.LogInformation("[Perf] ImageProcessing skip (非GIF): {Elapsed}ms, ext={Ext}", sw.ElapsedMilliseconds, ext);
            return null;
        }

        try
        {
            var loadSw = Stopwatch.StartNew();
            using var image = await Image.LoadAsync(file.OpenReadStream());
            var loadMs = loadSw.ElapsedMilliseconds;

            var encoder = new WebpEncoder
            {
                Quality = _webpQuality,
                NearLossless = image.Frames.Count <= 1,
                NearLosslessQuality = _webpQuality,
            };

            var encodeSw = Stopwatch.StartNew();
            var ms = new MemoryStream();
            await image.SaveAsWebpAsync(ms, encoder);
            ms.Position = 0;
            var encodeMs = encodeSw.ElapsedMilliseconds;

            var originalSize = file.Length;
            var compressedSize = ms.Length;

            if (compressedSize >= originalSize)
            {
                logger.LogDebug(
                    "GIF → WebP 转换后体积未减小 ({Original} → {Compressed})，保留原始文件",
                    originalSize, compressedSize);
                return null;
            }

            logger.LogInformation("[Perf] GIF→WebP Load={LoadMs}ms Encode={EncodeMs}ms 总={TotalMs}ms {OrigSize}→{NewSize} frames={Frames}",
                loadMs, encodeMs, sw.ElapsedMilliseconds, originalSize, compressedSize, image.Frames.Count);

            var newFileName = Path.ChangeExtension(file.FileName, ".webp");
            return new ProcessedFormFile(ms, newFileName, "image/webp");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Perf] GIF→WebP 转换失败, 已耗时: {Elapsed}ms", sw.ElapsedMilliseconds);
            return null;
        }
    }

    /// <summary>
    /// 流式处理 GIF → WebP。接收原始流，返回转换后的 MemoryStream。
    /// 非 GIF 文件调用此方法时直接返回原始流（不做转换）。
    /// </summary>
    /// <returns>(转换后的流, 新文件名, 新 ContentType)</returns>
    public async Task<(Stream stream, string fileName, string contentType)> ProcessStreamAsync(
        Stream inputStream, string fileName, string? contentType)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext is not ".gif")
        {
            return (inputStream, fileName, contentType ?? "application/octet-stream");
        }

        var sw = Stopwatch.StartNew();

        try
        {
            var loadSw = Stopwatch.StartNew();
            using var image = await Image.LoadAsync(inputStream);
            var loadMs = loadSw.ElapsedMilliseconds;

            var encoder = new WebpEncoder
            {
                Quality = _webpQuality,
                NearLossless = image.Frames.Count <= 1,
                NearLosslessQuality = _webpQuality,
            };

            var encodeSw = Stopwatch.StartNew();
            var ms = new MemoryStream();
            await image.SaveAsWebpAsync(ms, encoder);
            ms.Position = 0;
            var encodeMs = encodeSw.ElapsedMilliseconds;

            logger.LogInformation("[Perf] Stream GIF→WebP Load={LoadMs}ms Encode={EncodeMs}ms 总={TotalMs}ms size={NewSize}",
                loadMs, encodeMs, sw.ElapsedMilliseconds, ms.Length);

            var newFileName = Path.ChangeExtension(fileName, ".webp");
            return (ms, newFileName, "image/webp");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Perf] Stream GIF→WebP 转换失败, 已耗时: {Elapsed}ms", sw.ElapsedMilliseconds);
            inputStream.Position = 0;
            return (inputStream, fileName, contentType ?? "application/octet-stream");
        }
    }

    private static int ParseQualityEnv(string key, int fallback)
    {
        var raw = Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process);
        return int.TryParse(raw, out var q) && q is >= 1 and <= 100 ? q : fallback;
    }
}

/// <summary>
/// 最小化的 <see cref="IFormFile"/> 实现，包装内存中的处理结果。
/// </summary>
internal sealed class ProcessedFormFile : IFormFile
{
    private readonly byte[] _content;

    public ProcessedFormFile(MemoryStream stream, string fileName, string contentType)
    {
        _content = stream.ToArray();
        FileName = fileName;
        ContentType = contentType;
    }

    public string ContentType { get; }
    public string ContentDisposition => $"form-data; name=\"file\"; filename=\"{FileName}\"";
    public IHeaderDictionary Headers => new HeaderDictionary
    {
        ["Content-Disposition"] = ContentDisposition,
        ["Content-Type"] = ContentType,
    };
    public long Length => _content.Length;
    public string Name => "file";
    public string FileName { get; }

    public Stream OpenReadStream() => new MemoryStream(_content, writable: false);

    public void CopyTo(Stream target) => target.Write(_content, 0, _content.Length);

    public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
        => target.WriteAsync(_content, 0, _content.Length, cancellationToken);
}
