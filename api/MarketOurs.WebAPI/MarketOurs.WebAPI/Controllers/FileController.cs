using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MarketOurs.WebAPI.Services;

namespace MarketOurs.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class FileController(IStorageService storageService, ILogger<FileController> logger) : ControllerBase
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    /// <summary>
    /// 上传单张图片
    /// </summary>
    [HttpPost("upload/image")]
    [Authorize]
    public async Task<ApiResponse<string>> UploadImage(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return ApiResponse<string>.Fail(400, "文件未找到");
        }

        var extension = Path.GetExtension(file.FileName).ToLower();
        if (!AllowedExtensions.Contains(extension))
        {
            return ApiResponse<string>.Fail(400, "不支持的文件类型，仅限图片 (jpg, png, gif, webp)");
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
            return ApiResponse<string>.Fail(500, "文件上传失败");
        }
    }

    /// <summary>
    /// 批量上传图片
    /// </summary>
    [HttpPost("upload/images")]
    [Authorize]
    public async Task<ApiResponse<List<string>>> UploadImages(List<IFormFile>? files)
    {
        if (files == null || files.Count == 0)
        {
            return ApiResponse<List<string>>.Fail(400, "未找到文件");
        }

        var urls = new List<string>();
        foreach (var file in files)
        {
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (AllowedExtensions.Contains(extension))
            {
                var url = await storageService.SaveFileAsync(file, "images");
                urls.Add(url);
            }
        }

        return ApiResponse<List<string>>.Success(urls, $"成功上传 {urls.Count} 张图片");
    }
}