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
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not ".gif") return null;

        try
        {
            using var image = await Image.LoadAsync(file.OpenReadStream());

            // 注意：即使是单帧 GIF，也转为 WebP — WebP 在单帧场景下仍然比
            // GIF 体积更小。多帧时自动编码为 Animated WebP。
            var encoder = new WebpEncoder
            {
                Quality = _webpQuality,
                // NearLossless 对动画 WebP 无效，所以只在单帧时启用微量 lossless 以提升文字清晰度
                NearLossless = image.Frames.Count <= 1,
                NearLosslessQuality = _webpQuality,
            };

            var ms = new MemoryStream();
            await image.SaveAsWebpAsync(ms, encoder);
            ms.Position = 0;

            var originalSize = file.Length;
            var compressedSize = ms.Length;

            if (compressedSize >= originalSize)
            {
                // 压缩后体积更大（极少见），保留原始文件
                logger.LogDebug(
                    "GIF → WebP 转换后体积未减小 ({Original} → {Compressed})，保留原始文件",
                    originalSize, compressedSize);
                return null;
            }

            logger.LogInformation(
                "GIF → Animated WebP 转换完成: {Original} bytes → {Compressed} bytes ({Ratio:P0} 减小), frames={Frames}",
                originalSize, compressedSize, 1.0 - (double)compressedSize / originalSize, image.Frames.Count);

            var newFileName = Path.ChangeExtension(file.FileName, ".webp");
            return new ProcessedFormFile(ms, newFileName, "image/webp");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GIF → WebP 转换失败，降级使用原始文件");
            return null;
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
