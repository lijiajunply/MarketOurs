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
public class FileController(IStorageService storageService, ILogger<FileController> logger) : ControllerBase
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];

    /// <summary>
    /// 上传单张图片 (限制为 jpg, png, gif, webp)
    /// </summary>
    /// <param name="file">上传的文件流</param>
    /// <returns>上传成功后的文件访问 URL</returns>
    [HttpPost("upload/image")]
    [Authorize]
    public async Task<ApiResponse<string>> UploadImage(IFormFile? file)
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
            // 保存到 images 子目录
            var url = await storageService.SaveFileAsync(file, "images");
            logger.LogInformation("用户上传图片成功: {Url}", url);
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
            var url = await storageService.SaveFileAsync(file, "avatars");
            logger.LogInformation("匿名上传头像成功: {Url}", url);
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
    /// <returns>成功上传的文件访问 URL 列表</returns>
    [HttpPost("upload/images")]
    [Authorize]
    public async Task<ApiResponse<List<string>>> UploadImages(List<IFormFile>? files)
    {
        if (files == null || files.Count == 0)
        {
            return ApiResponse<List<string>>.Fail(ErrorCode.FileNotFound);
        }

        var urls = new List<string>();
        foreach (var file in from file in files
                 let extension = Path.GetExtension(file.FileName).ToLower()
                 where AllowedExtensions.Contains(extension)
                 select file)
        {
            var url = await storageService.SaveFileAsync(file, "images");
            urls.Add(url);
        }

        return ApiResponse<List<string>>.Success(urls, $"成功上传 {urls.Count} 张图片");
    }
}