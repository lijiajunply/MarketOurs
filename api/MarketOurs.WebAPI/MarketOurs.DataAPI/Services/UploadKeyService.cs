using System.Text.Json;
using MarketOurs.DataAPI.Configs;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MarketOurs.DataAPI.Services;

/// <summary>
/// 管理上传密钥（Upload Key）的生命周期。
/// 上传密钥用于将临时上传的文件与后续操作（如创建帖子）关联起来，
/// 确保操作失败或超时时能够清理孤立的文件。
/// </summary>
public class UploadKeyService(
    IDistributedCache distributedCache,
    IEnumerable<IConnectionMultiplexer> redisEnumerable,
    ILogger<UploadKeyService> logger)
{
    private readonly IConnectionMultiplexer? _redis = redisEnumerable.FirstOrDefault();
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(30);

    private sealed record UploadKeyEntry(List<string> Urls, DateTime CreatedAt);

    /// <summary>
    /// 生成一个新的上传密钥
    /// </summary>
    /// <param name="ttl">密钥有效期，默认 30 分钟</param>
    /// <returns>包含 key 和 expiresIn（秒）的元组</returns>
    public async Task<(string Key, int ExpiresIn)> GenerateKeyAsync(TimeSpan? ttl = null)
    {
        var effectiveTtl = ttl ?? DefaultTtl;
        var key = Guid.NewGuid().ToString("N");

        var entry = new UploadKeyEntry([], DateTime.UtcNow);
        var json = JsonSerializer.Serialize(entry);

        var cacheKey = CacheKeys.UploadKey(key);
        await distributedCache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = effectiveTtl
        });

        logger.LogInformation("Generated upload key: {Key}, TTL: {Ttl}s", key, (int)effectiveTtl.TotalSeconds);
        return (key, (int)effectiveTtl.TotalSeconds);
    }

    /// <summary>
    /// 将文件 URL 关联到指定的上传密钥
    /// </summary>
    public async Task TrackFileAsync(string key, string url)
    {
        await TrackFilesAsync(key, [url]);
    }

    /// <summary>
    /// 将多个文件 URL 关联到指定的上传密钥。
    /// 批量写入避免并行上传后多个 TrackFileAsync 读写同一个缓存项时互相覆盖。
    /// </summary>
    public async Task TrackFilesAsync(string key, IEnumerable<string> urls)
    {
        var cacheKey = CacheKeys.UploadKey(key);
        var urlList = urls.Where(url => !string.IsNullOrWhiteSpace(url)).ToList();
        if (urlList.Count == 0) return;

        var json = await distributedCache.GetStringAsync(cacheKey);

        UploadKeyEntry entry;
        if (string.IsNullOrWhiteSpace(json))
        {
            logger.LogWarning("Upload key {Key} not found or expired when tracking {Count} files", key, urlList.Count);
            entry = new UploadKeyEntry([], DateTime.UtcNow);
        }
        else
        {
            entry = JsonSerializer.Deserialize<UploadKeyEntry>(json) ?? new UploadKeyEntry([], DateTime.UtcNow);
        }

        entry.Urls.AddRange(urlList);
        var updatedJson = JsonSerializer.Serialize(entry);

        // 刷新 TTL：每次添加文件时重置过期时间
        await distributedCache.SetStringAsync(cacheKey, updatedJson, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = DefaultTtl
        });

        logger.LogDebug("Tracked {Count} files under upload key {Key}", urlList.Count, key);
    }

    /// <summary>
    /// 获取上传密钥关联的文件列表，并删除该密钥（确认操作）
    /// </summary>
    /// <returns>关联的文件 URL 列表，若密钥不存在或已过期则返回空列表</returns>
    public async Task<List<string>> GetAndRemoveFilesAsync(string key)
    {
        var cacheKey = CacheKeys.UploadKey(key);
        var json = await distributedCache.GetStringAsync(cacheKey);

        if (string.IsNullOrWhiteSpace(json))
        {
            logger.LogWarning("Upload key {Key} not found or already expired", key);
            return [];
        }

        await distributedCache.RemoveAsync(cacheKey);

        var entry = JsonSerializer.Deserialize<UploadKeyEntry>(json);
        var urls = entry?.Urls ?? [];
        logger.LogInformation("Confirmed upload key {Key} with {Count} files", key, urls.Count);
        return urls;
    }

    /// <summary>
    /// 删除上传密钥并清理其关联的所有文件
    /// </summary>
    public async Task DeleteFilesByKeyAsync(string key, IStorageService storageService)
    {
        var urls = await GetAndRemoveFilesAsync(key);
        if (urls.Count == 0) return;

        try
        {
            var deleted = await storageService.DeleteFilesAsync(urls);
            logger.LogInformation("Cleaned up {Deleted}/{Total} files for expired upload key {Key}",
                deleted, urls.Count, key);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to clean up files for upload key {Key}", key);
        }
    }

    /// <summary>
    /// 获取所有已过期的上传密钥（通过 Redis KEYS 扫描）
    /// 注意：此方法需要 IConnectionMultiplexer 来执行 SCAN 操作
    /// </summary>
    /// <returns>过期的密钥 ID 列表</returns>
    public async Task<List<string>> GetActiveKeysAsync()
    {
        if (_redis == null) return [];

        var result = new List<string>();
        var pattern = CacheKeys.UploadKeyPattern();

        // 使用 SCAN 而非 KEYS，避免阻塞 Redis
        var endpoints = _redis.GetEndPoints();
        foreach (var endpoint in endpoints)
        {
            var server = _redis.GetServer(endpoint);
            if (!server.IsConnected) continue;

            await foreach (var redisKey in server.KeysAsync(pattern: pattern))
            {
                // 去掉前缀，只保留 key ID
                var fullKey = redisKey.ToString();
                var prefix = "upload_key:";
                var id = fullKey[(fullKey.IndexOf(prefix, StringComparison.Ordinal) + prefix.Length)..];
                result.Add(id);
            }
        }

        return result;
    }
}
