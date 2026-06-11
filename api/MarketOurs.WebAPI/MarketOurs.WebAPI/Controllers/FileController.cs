using MarketOurs.DataAPI.Exceptions;
using MarketOurs.DataAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        var (key, expiresIn) = await uploadKeyService.GenerateKeyAsync();
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

        try
        {
            // GIF → Animated WebP 转换（非 GIF 原样放行）
            var processed = await imageProcessingService.ProcessAsync(file);

            // 保存到 images 子目录
            var url = await storageService.SaveFileAsync(processed ?? file, "images");
            logger.LogInformation("用户上传图片成功: {Url}", url);

            // 释放处理后的临时内存流
            (processed as IDisposable)?.Dispose();

            // 如果提供了上传密钥，将文件 URL 关联到该密钥
            if (!string.IsNullOrWhiteSpace(key))
            {
                await uploadKeyService.TrackFileAsync(key, url);
            }

            return ApiResponse<string>.Success(url, "上传成功");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "文件上传过程中发生错误");
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

        try
        {
            var processed = await imageProcessingService.ProcessAsync(file);

            var url = await storageService.SaveFileAsync(processed ?? file, "avatars");
            logger.LogInformation("匿名上传头像成功: {Url}", url);

            (processed as IDisposable)?.Dispose();

            return ApiResponse<string>.Success(url, "上传成功");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "头像上传过程中发生错误");
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
        // 原先串行 foreach 导致 N 张图片 = N 次串行 S3/存储网络往返
        var validFiles = (from file in files
            let extension = Path.GetExtension(file.FileName).ToLower()
            where AllowedExtensions.Contains(extension)
            select file).ToList();

        var uploadTasks = validFiles.Select(async file =>
        {
            var processed = await imageProcessingService.ProcessAsync(file);
            var url = await storageService.SaveFileAsync(processed ?? file, "images");
            (processed as IDisposable)?.Dispose();
            return url;
        });

        var urls = (await Task.WhenAll(uploadTasks)).ToList();

        // 如果提供了上传密钥，一次性追踪所有文件 URL 到 Redis
        if (!string.IsNullOrWhiteSpace(key))
        {
            await uploadKeyService.TrackFilesAsync(key, urls);
        }

        return ApiResponse<List<string>>.Success(urls, $"成功上传 {urls.Count} 张图片");
    }
}
