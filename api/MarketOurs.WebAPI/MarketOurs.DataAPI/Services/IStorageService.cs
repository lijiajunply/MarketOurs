using Microsoft.AspNetCore.Http;

namespace MarketOurs.DataAPI.Services;

public interface IStorageService
{
    /// <summary>
    /// 保存文件并返回文件的可访问 URL
    /// </summary>
    Task<string> SaveFileAsync(IFormFile file, string subFolder = "uploads");

    /// <summary>
    /// 直接从流保存文件，绕过 IFormFile 缓冲。用于流式上传。
    /// </summary>
    Task<string> SaveStreamAsync(Stream stream, string fileName, string contentType, string subFolder = "uploads");

    /// <summary>
    /// 删除文件
    /// </summary>
    Task<bool> DeleteFileAsync(string fileUrl);

    /// <summary>
    /// 批量删除文件，返回成功删除的数量
    /// </summary>
    Task<int> DeleteFilesAsync(IEnumerable<string> fileUrls);
}