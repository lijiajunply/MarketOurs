using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MarketOurs.WebAPI.Services;

public interface IStorageService
{
    /// <summary>
    /// 保存文件并返回文件的可访问 URL
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

        if (!Directory.Exists(_uploadRoot))
        {
            Directory.CreateDirectory(_uploadRoot);
        }

        var targetFolder = Path.Combine(_uploadRoot, subFolder);
        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(targetFolder, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        logger.LogInformation("文件已保存到本地: {FilePath}", filePath);
        return $"/uploads/{subFolder}/{fileName}".Replace("\\", "/");
    }

    public Task<bool> DeleteFileAsync(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl)) return Task.FromResult(false);

        try
        {
            var relativePath = fileUrl.TrimStart('/');
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

public class VercelBlobStorageService(
    HttpClient httpClient,
    LocalStorageService localStorageService,
    ILogger<VercelBlobStorageService> logger) : IStorageService
{
    private const string BlobUploadBaseUrl = "https://blob.vercel-storage.com";
    private const string BlobApiBaseUrl = "https://vercel.com/api/blob";
    private readonly string _token = GetRequiredEnv("BLOB_READ_WRITE_TOKEN");
    private readonly string _storeId =
        Environment.GetEnvironmentVariable("BLOB_STORE_ID", EnvironmentVariableTarget.Process)
        ?? ParseStoreId(GetRequiredEnv("BLOB_READ_WRITE_TOKEN"));
    private readonly string _access =
        Environment.GetEnvironmentVariable("BLOB_ACCESS", EnvironmentVariableTarget.Process) ?? "public";
    private readonly string _basePath =
        (Environment.GetEnvironmentVariable("BLOB_BASE_PATH", EnvironmentVariableTarget.Process) ?? "uploads").Trim('/');
    private readonly int _cacheControlMaxAgeSeconds =
        int.TryParse(
            Environment.GetEnvironmentVariable("BLOB_CACHE_CONTROL_MAX_AGE", EnvironmentVariableTarget.Process),
            out var cacheSeconds)
            ? cacheSeconds
            : 31536000;

    public async Task<string> SaveFileAsync(IFormFile file, string subFolder = "uploads")
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("文件为空");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var pathname = BuildPathname(subFolder, fileName);
        var requestUri = $"{BlobUploadBaseUrl}/{pathname}";

        using var request = new HttpRequestMessage(HttpMethod.Put, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        request.Headers.Add("x-api-version", "12");
        request.Headers.Add("x-api-blob-request-id", Guid.NewGuid().ToString("N"));
        request.Headers.Add("x-vercel-blob-store-id", _storeId);
        request.Headers.Add("access", _access);
        request.Headers.Add("x-add-random-suffix", "0");
        request.Headers.Add("x-allow-overwrite", "0");
        request.Headers.Add("x-cache-control-max-age", _cacheControlMaxAgeSeconds.ToString());
        request.Headers.Add("x-content-length", file.Length.ToString());
        request.Content = new StreamContent(file.OpenReadStream());
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        request.Content.Headers.ContentLength = file.Length;
        request.Headers.Add("x-content-type", request.Content.Headers.ContentType.MediaType);

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Vercel Blob 上传失败: {StatusCode} {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException("上传到 Vercel Blob 失败。");
        }

        using var document = JsonDocument.Parse(responseBody);
        if (!document.RootElement.TryGetProperty("url", out var urlElement))
        {
            logger.LogError("Vercel Blob 上传响应缺少 url 字段: {Body}", responseBody);
            throw new InvalidOperationException("Vercel Blob 返回了无效响应。");
        }

        var url = urlElement.GetString();
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException("Vercel Blob 返回了空 URL。");
        }

        logger.LogInformation("文件已上传到 Vercel Blob: {Url}", url);
        return url;
    }

    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return false;
        }

        if (!Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri) || !uri.Host.EndsWith(".blob.vercel-storage.com"))
        {
            return await localStorageService.DeleteFileAsync(fileUrl);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BlobApiBaseUrl}/delete");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        request.Headers.Add("x-api-version", "12");
        request.Headers.Add("x-api-blob-request-id", Guid.NewGuid().ToString("N"));
        request.Headers.Add("x-vercel-blob-store-id", _storeId);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { urls = new[] { fileUrl } }),
            Encoding.UTF8,
            "application/json");

        using var response = await httpClient.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("Vercel Blob 文件已删除: {Url}", fileUrl);
            return true;
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        logger.LogError("Vercel Blob 删除失败: {StatusCode} {Body}", response.StatusCode, responseBody);
        return false;
    }

    private string BuildPathname(string subFolder, string fileName)
    {
        var parts = new[] { _basePath, subFolder.Trim('/'), fileName }
            .Where(part => !string.IsNullOrWhiteSpace(part));
        return string.Join('/', parts);
    }

    private static string ParseStoreId(string token)
    {
        var parts = token.Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 4 && !string.IsNullOrWhiteSpace(parts[3]))
        {
            return parts[3];
        }

        throw new InvalidOperationException(
            "无法从 BLOB_READ_WRITE_TOKEN 解析 store id，请显式配置 BLOB_STORE_ID。");
    }

    private static string GetRequiredEnv(string key)
    {
        return Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process)
               ?? throw new InvalidOperationException($"缺少 {key} 配置。");
    }
}
