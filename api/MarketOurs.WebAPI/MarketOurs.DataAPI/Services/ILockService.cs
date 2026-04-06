using StackExchange.Redis;

namespace MarketOurs.DataAPI.Services;

/// <summary>
/// 分布式锁服务接口，用于在多节点环境下协调并发操作
/// </summary>
public interface ILockService
{
    /// <summary>
    /// 获取分布式锁
    /// </summary>
    /// <param name="key">锁的Key</param>
    /// <param name="value">锁的值 (通常为 Guid)</param>
    /// <param name="expiry">锁的有效时间</param>
    /// <returns>是否成功获取锁</returns>
    Task<bool> AcquireAsync(string key, string value, TimeSpan expiry);

    /// <summary>
    /// 释放分布式锁
    /// </summary>
    /// <param name="key">锁的Key</param>
    /// <param name="value">锁的值 (需与获取时一致)</param>
    /// <returns>是否成功释放锁</returns>
    Task<bool> ReleaseAsync(string key, string value);
}

/// <summary>
/// 基于 Redis 实现的分布式锁服务
/// </summary>
public class RedisLockService(IEnumerable<IConnectionMultiplexer> redisEnumerable) : ILockService
{
    private readonly IConnectionMultiplexer? _redis = redisEnumerable.FirstOrDefault();

    /// <inheritdoc/>
    public async Task<bool> AcquireAsync(string key, string value, TimeSpan expiry)
    {
        if (_redis == null) return true; // Fail-safe: No Redis, no lock (assume success in development)
        var db = _redis.GetDatabase();
        return await db.LockTakeAsync(key, value, expiry);
    }

    public async Task<bool> ReleaseAsync(string key, string value)
    {
        if (_redis == null) return true;
        var db = _redis.GetDatabase();
        return await db.LockReleaseAsync(key, value);
    }
}