using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IO;

namespace MarketOurs.WebAPI.Services;

public interface IStorageService
{
    /// <summary>
    /// 保存文件并返回文件的相对URL路径
    /// </summary>
    Task<string> SaveFileAsync(IFormFile file, string subFolder = "uploads");
    
    /// <summary>
    /// 删除文件
    /// </summary>
    Task<bool> DeleteFileAsync(string fileUrl);
}

public class LocalStorageService(IWebHostEnvironment environment, ILogger<LocalStorageService> logger) : IStorageService
{
    private readonly string _uploadRoot = Path.Combine(environment.WebRootPath ?? "wwwroot", "uploads");

    public async Task<string> SaveFileAsync(IFormFile file, string subFolder = "uploads")
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("文件为空");

        // 确保根目录存在
        if (!Directory.Exists(_uploadRoot))
        {
            Directory.CreateDirectory(_uploadRoot);
        }

        // 处理子目录
        var targetFolder = Path.Combine(_uploadRoot, subFolder);
        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        // 生成唯一文件名
        var extension = Path.GetExtension(file.FileName).ToLower();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(targetFolder, fileName);

        // 保存文件
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        logger.LogInformation("文件已保存到本地: {FilePath}", filePath);

        // 返回可访问的相对路径
        return $"/uploads/{subFolder}/{fileName}".Replace("\\", "/");
    }

    public Task<bool> DeleteFileAsync(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl)) return Task.FromResult(false);

        try
        {
            // 将URL路径转换为物理路径
            var relativePath = fileUrl.TrimStart('/');
            // 假设 uploads 是在 wwwroot 下
            var filePath = Path.Combine(environment.WebRootPath ?? "wwwroot", relativePath);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                logger.LogInformation("文件已删除: {FilePath}", filePath);
                return Task.FromResult(true);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "删除文件失败: {FileUrl}", fileUrl);
        }

        return Task.FromResult(false);
    }
}
