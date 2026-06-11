using System.Diagnostics;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using MarketOurs.DataAPI.Configs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MarketOurs.DataAPI.Services;

public class S3StorageService(
    IAmazonS3 s3Client,
    LocalStorageService localStorageService,
    ILogger<S3StorageService> logger,
    S3StorageConfig config) : IStorageService
{
    private const int MaxBatchDeleteSize = 1000;

    /// <summary>
    /// TransferUtility 分片大小 (5MB)，超过此大小的文件自动走分片并行上传。
    /// </summary>
    private const int PartSize = 5 * 1024 * 1024;

    public async Task<string> SaveFileAsync(IFormFile file, string subFolder = "uploads")
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("文件为空");

        var sw = Stopwatch.StartNew();

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var key = BuildKey(subFolder, fileName);
        var contentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType;

        await using var stream = file.OpenReadStream();

        await UploadStreamToS3Async(stream, key, contentType, file.Length);

        var url = BuildAccessUrl(key);
        logger.LogInformation("[Perf] S3 SaveFile 总={TotalMs}ms size={Size}KB key={Key}",
            sw.ElapsedMilliseconds, file.Length / 1024, key);
        return url;
    }

    public async Task<string> SaveStreamAsync(
        Stream stream, string fileName, string contentType, string subFolder = "uploads")
    {
        if (stream == null)
            throw new ArgumentException("流为空");

        var sw = Stopwatch.StartNew();

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var keyName = $"{Guid.NewGuid():N}{extension}";
        var key = BuildKey(subFolder, keyName);

        await UploadStreamToS3Async(stream, key, contentType, stream.CanSeek ? stream.Length : -1);

        var url = BuildAccessUrl(key);
        logger.LogInformation("[Perf] S3 SaveStream 总={TotalMs}ms key={Key}", sw.ElapsedMilliseconds, key);
        return url;
    }

    /// <summary>
    /// 将流上传到 S3/COS。
    /// 已知长度的流：自动分片并行上传（TransferUtility）。
    /// 未知长度的流：单次 PutObject。
    /// </summary>
    private async Task UploadStreamToS3Async(Stream stream, string key, string contentType, long contentLength)
    {
        // 已知长度且大于分片上限：使用 TransferUtility 分片并行上传
        if (contentLength > PartSize)
        {
            var putSw = Stopwatch.StartNew();
            using var transferUtility = new TransferUtility(s3Client);

            var request = new TransferUtilityUploadRequest
            {
                BucketName = config.BucketName,
                Key = key,
                InputStream = stream,
                ContentType = contentType,
                PartSize = PartSize,
                AutoCloseStream = false,
            };

            await transferUtility.UploadAsync(request);
            logger.LogInformation("[Perf] S3 TransferUtility(分片) 耗时={PutMs}ms size={Size}KB key={Key}",
                putSw.ElapsedMilliseconds, contentLength / 1024, key);
        }
        else
        {
            var putSw = Stopwatch.StartNew();
            var request = new PutObjectRequest
            {
                BucketName = config.BucketName,
                Key = key,
                InputStream = stream,
                ContentType = contentType,
                AutoCloseStream = false,
            };

            var response = await s3Client.PutObjectAsync(request);
            logger.LogInformation("[Perf] S3 PutObject(普通) 耗时={PutMs}ms size={Size}KB key={Key}",
                putSw.ElapsedMilliseconds, contentLength / 1024, key);

            if ((int)response.HttpStatusCode < 200 || (int)response.HttpStatusCode >= 300)
            {
                logger.LogError("S3 上传失败: {StatusCode}", response.HttpStatusCode);
                throw new InvalidOperationException($"上传到 S3 失败: {(int)response.HttpStatusCode}");
            }
        }
    }

    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return false;

        var key = ExtractKeyFromUrl(fileUrl);
        if (key == null)
            return await localStorageService.DeleteFileAsync(fileUrl);

        var request = new DeleteObjectRequest
        {
            BucketName = config.BucketName,
            Key = key
        };

        var response = await s3Client.DeleteObjectAsync(request);
        if ((int)response.HttpStatusCode >= 200 && (int)response.HttpStatusCode < 300)
        {
            logger.LogInformation("S3 文件已删除: {Key}", key);
            return true;
        }

        logger.LogError("S3 删除失败: {StatusCode}", response.HttpStatusCode);
        return false;
    }

    public async Task<int> DeleteFilesAsync(IEnumerable<string> fileUrls)
    {
        var urls = fileUrls.ToList();
        if (urls.Count == 0) return 0;

        // Separate S3 keys from local URLs
        var s3Entries = new List<(string Url, string Key)>();
        var localUrls = new List<string>();

        foreach (var url in urls)
        {
            var key = ExtractKeyFromUrl(url);
            if (key != null) s3Entries.Add((url, key));
            else localUrls.Add(url);
        }

        var deleted = 0;

        // Batch delete from S3 (up to 1000 per request)
        foreach (var batch in s3Entries.Chunk(MaxBatchDeleteSize))
        {
            try
            {
                var request = new DeleteObjectsRequest
                {
                    BucketName = config.BucketName,
                    Objects = batch.Select(e => new KeyVersion { Key = e.Key }).ToList()
                };

                var response = await s3Client.DeleteObjectsAsync(request);
                deleted += response.DeletedObjects.Count;
                if (response.DeleteErrors.Count > 0)
                {
                    logger.LogWarning("S3 batch delete had {ErrorCount} errors",
                        response.DeleteErrors.Count);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "S3 batch delete failed for {Count} objects", batch.Length);
            }
        }

        // Fall back to local deletes
        foreach (var localUrl in localUrls)
        {
            if (await localStorageService.DeleteFileAsync(localUrl)) deleted++;
        }

        logger.LogInformation("S3 batch delete completed: {Deleted}/{Total}", deleted, urls.Count);
        return deleted;
    }

    private string BuildKey(string subFolder, string fileName)
    {
        var parts = new[] { config.BasePrefix, subFolder.Trim('/'), fileName }
            .Where(part => !string.IsNullOrWhiteSpace(part));
        return string.Join('/', parts);
    }

    private string BuildAccessUrl(string key)
    {
        if (!string.IsNullOrWhiteSpace(config.CdnBaseUrl))
            return $"{config.CdnBaseUrl.TrimEnd('/')}/{key}";

        if (!string.IsNullOrWhiteSpace(config.Endpoint))
            return $"{config.Endpoint.TrimEnd('/')}/{config.BucketName}/{key}";

        return $"https://{config.BucketName}.s3.{config.Region}.amazonaws.com/{key}";
    }

    private string? ExtractKeyFromUrl(string fileUrl)
    {
        if (!Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri))
            return null;

        if (!string.IsNullOrWhiteSpace(config.CdnBaseUrl) &&
            fileUrl.StartsWith(config.CdnBaseUrl, StringComparison.OrdinalIgnoreCase))
        {
            return fileUrl[config.CdnBaseUrl.TrimEnd('/').Length..].TrimStart('/');
        }

        if (uri.Host.Contains("s3") || uri.Host.Contains("amazonaws.com") ||
            (!string.IsNullOrWhiteSpace(config.Endpoint) &&
             fileUrl.StartsWith(config.Endpoint, StringComparison.OrdinalIgnoreCase)))
        {
            var path = uri.AbsolutePath.TrimStart('/');
            if (config.ForcePathStyle && path.StartsWith(config.BucketName + "/"))
                return path[(config.BucketName.Length + 1)..];
            return path;
        }

        return null;
    }
}
