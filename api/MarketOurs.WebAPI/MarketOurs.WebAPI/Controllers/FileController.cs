using System.Diagnostics;
using MarketOurs.DataAPI.Exceptions;
using MarketOurs.DataAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace MarketOurs.WebAPI.Controllers;

/// <summary>
/// 文件控制器，处理图片等媒体文件的上传与存储
/// </summary>
[ApiController]
[Route("[controller]")]
public class FileController(
    IStorageService storageService,
    ImageProcessingService imageProcessingService,
    UploadKeyService uploadKeyService,
    ILogger<FileController> logger) : ControllerBase
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];

    /// <summary>
    /// 生成上传密钥，用于关联后续上传的图片与创建操作
    /// </summary>
    /// <returns>上传密钥及有效期</returns>
    [HttpPost("upload/key")]
    [Authorize]
    public async Task<ApiResponse<object>> GenerateUploadKey()
    {
        var sw = Stopwatch.StartNew();
        var (key, expiresIn) = await uploadKeyService.GenerateKeyAsync();
        logger.LogInformation("[Perf] GenerateUploadKey 总耗时: {Elapsed}ms, key={Key}", sw.ElapsedMilliseconds, key);
        return ApiResponse<object>.Success(new { key, expiresIn }, "上传密钥已生成");
    }

    /// <summary>
    /// 上传单张图片 (限制为 jpg, png, gif, webp)
    /// </summary>
    /// <param name="file">上传的文件流</param>
    /// <param name="key">可选的上传密钥，用于关联到后续的创建操作</param>
    /// <returns>上传成功后的文件访问 URL</returns>
    [HttpPost("upload/image")]
    [Authorize]
    public async Task<ApiResponse<string>> UploadImage(IFormFile? file, [FromQuery] string? key = null)
    {
        if (file == null || file.Length == 0)
        {
            return ApiResponse<string>.Fail(ErrorCode.FileNotFound);
        }

        var extension = Path.GetExtension(file.FileName).ToLower();
        if (!AllowedExtensions.Contains(extension))
        {
            return ApiResponse<string>.Fail(ErrorCode.UnsupportedFileType);
        }

        var totalSw = Stopwatch.StartNew();

        try
        {
            var processSw = Stopwatch.StartNew();
            var processed = await imageProcessingService.ProcessAsync(file);
            logger.LogInformation("[Perf] ImageProcessingService.ProcessAsync 耗时: {Elapsed}ms, ext={Ext}, size={Size}",
                processSw.ElapsedMilliseconds, extension, file.Length);

            var saveSw = Stopwatch.StartNew();
            var url = await storageService.SaveFileAsync(processed ?? file, "images");
            logger.LogInformation("[Perf] StorageService.SaveFileAsync 耗时: {Elapsed}ms, url={Url}",
                saveSw.ElapsedMilliseconds, url);

            (processed as IDisposable)?.Dispose();

            if (!string.IsNullOrWhiteSpace(key))
            {
                var trackSw = Stopwatch.StartNew();
                await uploadKeyService.TrackFileAsync(key, url);
                logger.LogInformation("[Perf] TrackFileAsync 耗时: {Elapsed}ms", trackSw.ElapsedMilliseconds);
            }

            logger.LogInformation("[Perf] UploadImage 总耗时: {Elapsed}ms, ext={Ext}, size={Size}KB, url={Url}",
                totalSw.ElapsedMilliseconds, extension, file.Length / 1024, url);
            return ApiResponse<string>.Success(url, "上传成功");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Perf] UploadImage 失败, 已耗时: {Elapsed}ms", totalSw.ElapsedMilliseconds);
            return ApiResponse<string>.Fail(ErrorCode.FileUploadFailed);
        }
    }

    /// <summary>
    /// 匿名上传头像（注册时使用），限制 2MB
    /// </summary>
    /// <param name="file">上传的文件流</param>
    /// <returns>上传成功后的文件访问 URL</returns>
    [HttpPost("upload/avatar")]
    [AllowAnonymous]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<ApiResponse<string>> UploadAvatar(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return ApiResponse<string>.Fail(ErrorCode.FileNotFound);
        }

        var extension = Path.GetExtension(file.FileName).ToLower();
        if (!AllowedExtensions.Contains(extension))
        {
            return ApiResponse<string>.Fail(ErrorCode.UnsupportedFileType);
        }

        var totalSw = Stopwatch.StartNew();

        try
        {
            var processSw = Stopwatch.StartNew();
            var processed = await imageProcessingService.ProcessAsync(file);
            logger.LogInformation("[Perf] Avatar ImageProcessingService.ProcessAsync 耗时: {Elapsed}ms",
                processSw.ElapsedMilliseconds);

            var saveSw = Stopwatch.StartNew();
            var url = await storageService.SaveFileAsync(processed ?? file, "avatars");
            logger.LogInformation("[Perf] Avatar StorageService.SaveFileAsync 耗时: {Elapsed}ms, url={Url}",
                saveSw.ElapsedMilliseconds, url);

            (processed as IDisposable)?.Dispose();

            logger.LogInformation("[Perf] UploadAvatar 总耗时: {Elapsed}ms, size={Size}KB, url={Url}",
                totalSw.ElapsedMilliseconds, file.Length / 1024, url);
            return ApiResponse<string>.Success(url, "上传成功");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Perf] UploadAvatar 失败, 已耗时: {Elapsed}ms", totalSw.ElapsedMilliseconds);
            return ApiResponse<string>.Fail(ErrorCode.FileUploadFailed);
        }
    }

    /// <summary>
    /// 批量上传图片
    /// </summary>
    /// <param name="files">上传的文件列表</param>
    /// <param name="key">可选的上传密钥，用于关联到后续的创建操作</param>
    /// <returns>成功上传的文件访问 URL 列表</returns>
    [HttpPost("upload/images")]
    [Authorize]
    public async Task<ApiResponse<List<string>>> UploadImages(List<IFormFile>? files, [FromQuery] string? key = null)
    {
        if (files == null || files.Count == 0)
        {
            return ApiResponse<List<string>>.Fail(ErrorCode.FileNotFound);
        }

        // 并行处理所有图片：GIF 转换 + 存储保存
        var validFiles = (from file in files
            let extension = Path.GetExtension(file.FileName).ToLower()
            where AllowedExtensions.Contains(extension)
            select file).ToList();

        var totalSw = Stopwatch.StartNew();

        var uploadTasks = validFiles.Select(async file =>
        {
            var ext = Path.GetExtension(file.FileName).ToLower();
            var fileSw = Stopwatch.StartNew();

            var processSw = Stopwatch.StartNew();
            var processed = await imageProcessingService.ProcessAsync(file);
            var processMs = processSw.ElapsedMilliseconds;

            var saveSw = Stopwatch.StartNew();
            var url = await storageService.SaveFileAsync(processed ?? file, "images");
            var saveMs = saveSw.ElapsedMilliseconds;

            (processed as IDisposable)?.Dispose();
            logger.LogInformation("[Perf] 批量上传单文件 处理={ProcessMs}ms 存储={SaveMs}ms 总={TotalMs}ms ext={Ext} size={Size}KB",
                processMs, saveMs, fileSw.ElapsedMilliseconds, ext, file.Length / 1024);
            return url;
        });

        var urls = (await Task.WhenAll(uploadTasks)).ToList();

        if (!string.IsNullOrWhiteSpace(key))
        {
            var trackSw = Stopwatch.StartNew();
            await uploadKeyService.TrackFilesAsync(key, urls);
            logger.LogInformation("[Perf] 批量TrackFilesAsync 耗时: {Elapsed}ms, count={Count}",
                trackSw.ElapsedMilliseconds, urls.Count);
        }

        logger.LogInformation("[Perf] UploadImages 总耗时: {Elapsed}ms, count={Count}",
            totalSw.ElapsedMilliseconds, urls.Count);
        return ApiResponse<List<string>>.Success(urls, $"成功上传 {urls.Count} 张图片");
    }

    /// <summary>
    /// 流式上传图片 — 绕过 IFormFile 缓冲，请求体直接流式写入 COS/S3。
    /// Content-Type 必须是 multipart/form-data，文件字段名统一为 "file"。
    /// </summary>
    [HttpPost("upload/stream")]
    [Authorize]
    [DisableFormModelBinding]
    public async Task<ApiResponse<List<string>>> UploadStream([FromQuery] string? key = null)
    {
        var contentType = Request.ContentType;
        if (string.IsNullOrWhiteSpace(contentType) ||
            !contentType.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponse<List<string>>.Fail(ErrorCode.UnsupportedFileType, "需要 multipart/form-data");
        }

        var boundary = HeaderUtilities.RemoveQuotes(
            MediaTypeHeaderValue.Parse(contentType).Boundary).Value;
        if (string.IsNullOrWhiteSpace(boundary))
        {
            return ApiResponse<List<string>>.Fail(ErrorCode.UnsupportedFileType, "无法获取 multipart boundary");
        }

        var totalSw = Stopwatch.StartNew();
        var urls = new List<string>();
        var reader = new MultipartReader(boundary, Request.Body);

        try
        {
            MultipartSection? section;
            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                var fileSw = Stopwatch.StartNew();

                if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var disposition))
                    continue;

                if (!disposition.DispositionType.Equals("form-data") ||
                    string.IsNullOrWhiteSpace(disposition.FileName.Value))
                    continue;

                var fileName = Path.GetFileName(disposition.FileName.Value.Trim('"'));
                var ext = Path.GetExtension(fileName).ToLower();

                if (!AllowedExtensions.Contains(ext))
                    continue;

                string url;

                if (ext is ".gif")
                {
                    // GIF 需要先完整读取再转 WebP（ImageSharp 不支持流式解码 GIF）
                    var processed = await imageProcessingService.ProcessStreamAsync(
                        section.Body, fileName, section.ContentType);
                    url = await storageService.SaveStreamAsync(
                        processed.stream, processed.fileName, processed.contentType, "images");
                }
                else
                {
                    // 非 GIF：流式直写 COS，不经过服务端缓冲
                    url = await storageService.SaveStreamAsync(
                        section.Body, fileName,
                        section.ContentType ?? "application/octet-stream",
                        "images");
                }

                urls.Add(url);
                logger.LogInformation("[Perf] 流式上传单文件 总={TotalMs}ms ext={Ext}",
                    fileSw.ElapsedMilliseconds, ext);
            }

            if (!string.IsNullOrWhiteSpace(key) && urls.Count > 0)
            {
                var trackSw = Stopwatch.StartNew();
                await uploadKeyService.TrackFilesAsync(key, urls);
                logger.LogInformation("[Perf] 流式TrackFiles 耗时: {Elapsed}ms", trackSw.ElapsedMilliseconds);
            }

            logger.LogInformation("[Perf] UploadStream 总耗时: {Elapsed}ms, count={Count}",
                totalSw.ElapsedMilliseconds, urls.Count);
            return ApiResponse<List<string>>.Success(urls, $"成功上传 {urls.Count} 张图片");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Perf] UploadStream 失败, 已耗时: {Elapsed}ms, 已上传: {Count}",
                totalSw.ElapsedMilliseconds, urls.Count);
            return ApiResponse<List<string>>.Fail(ErrorCode.FileUploadFailed);
        }
    }
}

/// <summary>
/// [DisableFormModelBinding] 属性 — 阻止 ASP.NET Core 自动解析 multipart/form-data，
/// 从而绕开 IFormFile 缓冲。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
internal sealed class DisableFormModelBindingAttribute : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var factories = context.ValueProviderFactories;
        factories.RemoveType<FormValueProviderFactory>();
        factories.RemoveType<FormFileValueProviderFactory>();
        factories.RemoveType<JQueryFormValueProviderFactory>();
    }

    public void OnResourceExecuted(ResourceExecutedContext context) { }
}
