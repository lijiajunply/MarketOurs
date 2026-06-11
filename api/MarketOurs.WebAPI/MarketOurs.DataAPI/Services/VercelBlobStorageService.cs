using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MarketOurs.DataAPI.Configs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MarketOurs.DataAPI.Services;

public class VercelBlobStorageService(
    HttpClient httpClient,
    LocalStorageService localStorageService,
    ILogger<VercelBlobStorageService> logger,
    VercelBlobConfig config) : IStorageService
{
    private const string BlobApiBaseUrl = "https://vercel.com/api/blob";

    public async Task<string> SaveFileAsync(IFormFile file, string subFolder = "uploads")
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("文件为空");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var pathname = BuildPathname(subFolder, fileName);
        var requestUri = $"{BlobApiBaseUrl}/?pathname={Uri.EscapeDataString(pathname)}";

        using var request = new HttpRequestMessage(HttpMethod.Put, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.Token);
        request.Headers.Add("x-api-version", "12");
        request.Headers.Add("x-api-blob-request-id", Guid.NewGuid().ToString("N"));
        request.Headers.Add("x-vercel-blob-store-id", config.StoreId);
        request.Headers.Add("x-vercel-blob-access", config.Access);
        request.Headers.Add("x-add-random-suffix", "0");
        request.Headers.Add("x-allow-overwrite", "0");
        request.Headers.Add("x-cache-control-max-age", config.CacheControlMaxAgeSeconds.ToString());
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
            throw new InvalidOperationException(
                $"上传到 Vercel Blob 失败: {(int)response.StatusCode} {response.ReasonPhrase}");
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

    public async Task<string> SaveStreamAsync(Stream stream, string fileName, string contentType, string subFolder = "uploads")
    {
        if (stream == null)
            throw new ArgumentException("流为空");

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var blobFileName = $"{Guid.NewGuid():N}{extension}";
        var pathname = BuildPathname(subFolder, blobFileName);
        var requestUri = $"{BlobApiBaseUrl}/?pathname={Uri.EscapeDataString(pathname)}";

        using var request = new HttpRequestMessage(HttpMethod.Put, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.Token);
        request.Headers.Add("x-api-version", "12");
        request.Headers.Add("x-api-blob-request-id", Guid.NewGuid().ToString("N"));
        request.Headers.Add("x-vercel-blob-store-id", config.StoreId);
        request.Headers.Add("x-vercel-blob-access", config.Access);
        request.Headers.Add("x-add-random-suffix", "0");
        request.Headers.Add("x-allow-overwrite", "0");
        request.Headers.Add("x-cache-control-max-age", config.CacheControlMaxAgeSeconds.ToString());
        request.Content = new StreamContent(stream);
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        request.Headers.Add("x-content-type", request.Content.Headers.ContentType.MediaType);

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Vercel Blob 流式上传失败: {StatusCode} {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException(
                $"上传到 Vercel Blob 失败: {(int)response.StatusCode} {response.ReasonPhrase}");
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

        logger.LogInformation("流式上传到 Vercel Blob: {Url}", url);
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
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.Token);
        request.Headers.Add("x-api-version", "12");
        request.Headers.Add("x-api-blob-request-id", Guid.NewGuid().ToString("N"));
        request.Headers.Add("x-vercel-blob-store-id", config.StoreId);
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

    public async Task<int> DeleteFilesAsync(IEnumerable<string> fileUrls)
    {
        var urls = fileUrls.ToList();
        if (urls.Count == 0) return 0;

        // Separate Vercel Blob URLs from local URLs
        var vercelUrls = new List<string>();
        var localUrls = new List<string>();

        foreach (var url in urls)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                uri.Host.EndsWith(".blob.vercel-storage.com"))
                vercelUrls.Add(url);
            else
                localUrls.Add(url);
        }

        var deleted = 0;

        // Batch delete from Vercel Blob (supports array of URLs)
        if (vercelUrls.Count > 0)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{BlobApiBaseUrl}/delete");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.Token);
                request.Headers.Add("x-api-version", "12");
                request.Headers.Add("x-api-blob-request-id", Guid.NewGuid().ToString("N"));
                request.Headers.Add("x-vercel-blob-store-id", config.StoreId);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(new { urls = vercelUrls }),
                    Encoding.UTF8,
                    "application/json");

                using var response = await httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    deleted += vercelUrls.Count;
                    logger.LogInformation("Vercel Blob batch deleted {Count} files", vercelUrls.Count);
                }
                else
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    logger.LogError("Vercel Blob batch delete failed: {StatusCode} {Body}",
                        response.StatusCode, responseBody);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Vercel Blob batch delete exception for {Count} files", vercelUrls.Count);
            }
        }

        // Fall back to local deletes
        foreach (var localUrl in localUrls)
        {
            if (await localStorageService.DeleteFileAsync(localUrl)) deleted++;
        }

        logger.LogInformation("Vercel Blob batch delete completed: {Deleted}/{Total}", deleted, urls.Count);
        return deleted;
    }

    private string BuildPathname(string subFolder, string fileName)
    {
        var parts = new[] { config.BaseUrl, subFolder.Trim('/'), fileName }
            .Where(part => !string.IsNullOrWhiteSpace(part));
        return string.Join('/', parts);
    }
}