using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace MarketOurs.WebAPI.Services;

/// <summary>
/// IP黑名单缓存服务接口
/// </summary>
public interface IIpBlacklistCacheService
{
    /// <summary>
    /// 检查IP是否在黑名单中
    /// </summary>
    Task<bool> IsIpBlacklistedAsync(string ip);

    /// <summary>
    /// 刷新黑名单缓存
    /// </summary>
    Task RefreshBlacklistAsync();

    /// <summary>
    /// 动态添加IP到黑名单
    /// </summary>
    Task AddToBlacklistAsync(string ip);

    /// <summary>
    /// 从黑名单中移除IP
    /// </summary>
    Task RemoveFromBlacklistAsync(string ip);

    /// <summary>
    /// 获取黑名单统计信息
    /// </summary>
    Task<BlacklistStats> GetStatsAsync();
}

/// <summary>
/// 黑名单统计信息
/// </summary>
public class BlacklistStats
{
    public int TotalIps { get; set; }
    public int TotalCidrRanges { get; set; }
    public long CacheHits { get; set; }
    public long CacheMisses { get; set; }
    public long BlacklistHits { get; set; }
    public DateTime LastRefreshTime { get; set; }
}

/// <summary>
/// IP黑名单缓存服务实现
/// </summary>
public class IpBlacklistCacheService(
    IDistributedCache distributedCache,
    IMemoryCache memoryCache,
    ILogger<IpBlacklistCacheService> logger,
    IConfiguration configuration)
    : IIpBlacklistCacheService
{
    private const string CacheKey = "ip_blacklist";
    private const string MemoryCacheKey = "ip_blacklist_memory";
    private const int RedisCacheExpirationMinutes = 20;
    private const int MemoryCacheExpirationMinutes = 2;

    // 并发控制锁
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    // 统计指标
    private long _cacheHits;
    private long _cacheMisses;
    private long _blacklistHits;
    private DateTime _lastRefreshTime = DateTime.UtcNow;

    /// <summary>
    /// IP黑名单数据模型（支持IP和CIDR）
    /// </summary>
    private class BlacklistData
    {
        public HashSet<string> ExactIps { get; set; } = [];
        public List<CidrRange> CidrRanges { get; set; } = [];
    }

    /// <summary>
    /// CIDR范围定义
    /// </summary>
    private class CidrRange
    {
        public string Cidr { get; set; } = string.Empty;
        public uint NetworkAddress { get; set; }
        public uint BroadcastAddress { get; set; }
    }

    #region 辅助方法

    /// <summary>
    /// 将IP地址字符串转换为uint
    /// </summary>
    private static uint IpToUInt(string ipAddress)
    {
        var ip = IPAddress.Parse(ipAddress);
        var bytes = ip.GetAddressBytes();
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return BitConverter.ToUInt32(bytes, 0);
    }

    /// <summary>
    /// 解析CIDR表示法
    /// </summary>
    private static CidrRange? ParseCidr(string cidr)
    {
        try
        {
            var parts = cidr.Split('/');
            if (parts.Length != 2) return null;

            var ip = parts[0];
            if (!int.TryParse(parts[1], out var prefixLength) || prefixLength < 0 || prefixLength > 32)
                return null;

            var ipUint = IpToUInt(ip);
            var mask = prefixLength == 0 ? 0 : 0xFFFFFFFF << (32 - prefixLength);
            var networkAddress = ipUint & mask;
            var broadcastAddress = networkAddress | ~mask;

            return new CidrRange
            {
                Cidr = cidr,
                NetworkAddress = networkAddress,
                BroadcastAddress = broadcastAddress
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 检查IP是否在CIDR范围内
    /// </summary>
    private static bool IsIpInCidrRange(string ip, CidrRange range)
    {
        try
        {
            var ipUint = IpToUInt(ip);
            return ipUint >= range.NetworkAddress && ipUint <= range.BroadcastAddress;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region 数据加载和缓存

    /// <summary>
    /// 从数据源加载IP黑名单
    /// </summary>
    private async Task<BlacklistData> LoadBlacklistFromSourceAsync()
    {
        var blacklistData = new BlacklistData();

        try
        {
            // 从配置文件加载黑名单
            var configBlacklist = configuration.GetSection("Security:IpBlacklist").Get<string[]>();
            if (configBlacklist is { Length: > 0 })
            {
                foreach (var entry in configBlacklist)
                {
                    if (string.IsNullOrWhiteSpace(entry)) continue;

                    var trimmed = entry.Trim();
                    if (trimmed.Contains('/'))
                    {
                        // CIDR格式
                        var cidrRange = ParseCidr(trimmed);
                        if (cidrRange != null)
                        {
                            blacklistData.CidrRanges.Add(cidrRange);
                        }
                    }
                    else
                    {
                        // 精确IP
                        blacklistData.ExactIps.Add(trimmed);
                    }
                }
            }

            // 从环境变量加载
            var envBlacklist = Environment.GetEnvironmentVariable("IP_BLACKLIST");
            if (!string.IsNullOrWhiteSpace(envBlacklist))
            {
                var entries = envBlacklist.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var entry in entries)
                {
                    var trimmed = entry.Trim();
                    if (trimmed.Contains('/'))
                    {
                        var cidrRange = ParseCidr(trimmed);
                        if (cidrRange != null)
                        {
                            blacklistData.CidrRanges.Add(cidrRange);
                        }
                    }
                    else
                    {
                        blacklistData.ExactIps.Add(trimmed);
                    }
                }
            }

            logger.LogInformation("从数据源加载了 {ExactCount} 个精确IP和 {CidrCount} 个CIDR范围到黑名单",
                blacklistData.ExactIps.Count, blacklistData.CidrRanges.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "加载IP黑名单时发生错误");
        }

        return await Task.FromResult(blacklistData);
    }

    /// <summary>
    /// 从Redis缓存获取IP黑名单（带优雅降级）
    /// </summary>
    private async Task<BlacklistData?> GetBlacklistFromRedisAsync()
    {
        try
        {
            var cachedData = await distributedCache.GetStringAsync(CacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var blacklistData = JsonSerializer.Deserialize<BlacklistData>(cachedData);
                logger.LogDebug("从Redis缓存读取到黑名单数据");
                return blacklistData;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "从Redis缓存读取IP黑名单时发生错误，将使用降级策略");
            // Redis故障时返回null，触发降级逻辑
        }

        return null;
    }

    /// <summary>
    /// 将IP黑名单保存到Redis缓存
    /// </summary>
    private async Task SaveBlacklistToRedisAsync(BlacklistData blacklistData)
    {
        try
        {
            var jsonData = JsonSerializer.Serialize(blacklistData);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(RedisCacheExpirationMinutes)
            };

            await distributedCache.SetStringAsync(CacheKey, jsonData, options);
            logger.LogInformation("成功将黑名单保存到Redis缓存，过期时间：{Minutes}分钟",
                RedisCacheExpirationMinutes);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "保存IP黑名单到Redis缓存时发生错误");
        }
    }

    /// <summary>
    /// 加载并写入缓存（调用方必须已持有 _refreshLock）
    /// </summary>
    private async Task<BlacklistData> LoadBlacklistDataInternalAsync()
    {
        var redisData = await GetBlacklistFromRedisAsync();
        BlacklistData blacklistData;

        if (redisData != null)
        {
            blacklistData = redisData;
        }
        else
        {
            blacklistData = await LoadBlacklistFromSourceAsync();
            await SaveBlacklistToRedisAsync(blacklistData);
        }

        memoryCache.Set(MemoryCacheKey, blacklistData, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(MemoryCacheExpirationMinutes),
            Size = 1
        });

        _lastRefreshTime = DateTime.UtcNow;
        return blacklistData;
    }

    /// <summary>
    /// 获取黑名单（支持双层缓存和并发控制）
    /// </summary>
    private async Task<BlacklistData> GetOrLoadBlacklistAsync()
    {
        // 1. 先从内存缓存获取（最快，无锁）
        if (memoryCache.TryGetValue<BlacklistData>(MemoryCacheKey, out var memoryData) && memoryData != null)
        {
            Interlocked.Increment(ref _cacheHits);
            return memoryData;
        }

        Interlocked.Increment(ref _cacheMisses);

        // 2. 使用锁防止缓存击穿
        await _refreshLock.WaitAsync();
        try
        {
            // 双重检查
            if (memoryCache.TryGetValue(MemoryCacheKey, out memoryData) && memoryData != null)
                return memoryData;

            return await LoadBlacklistDataInternalAsync();
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    #endregion

    #region 公共接口实现

    /// <summary>
    /// 检查IP是否在黑名单中
    /// </summary>
    public async Task<bool> IsIpBlacklistedAsync(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            return false;
        }

        try
        {
            var blacklistData = await GetOrLoadBlacklistAsync();

            // 先检查精确匹配
            if (blacklistData.ExactIps.Contains(ip))
            {
                Interlocked.Increment(ref _blacklistHits);
                return true;
            }

            // 再检查CIDR范围匹配
            foreach (var cidrRange in blacklistData.CidrRanges)
            {
                if (IsIpInCidrRange(ip, cidrRange))
                {
                    Interlocked.Increment(ref _blacklistHits);
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "检查IP黑名单时发生错误：{Ip}", ip);
            return false;
        }
    }

    /// <summary>
    /// 刷新黑名单缓存
    /// </summary>
    public async Task RefreshBlacklistAsync()
    {
        await _refreshLock.WaitAsync();
        try
        {
            logger.LogInformation("开始刷新IP黑名单缓存");
            // 主动失效内存缓存，强制从数据源重新加载
            memoryCache.Remove(MemoryCacheKey);
            await LoadBlacklistDataInternalAsync();
            logger.LogInformation("IP黑名单缓存刷新完成");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "刷新IP黑名单缓存时发生错误");
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <summary>
    /// 动态添加IP到黑名单
    /// </summary>
    public async Task AddToBlacklistAsync(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            throw new ArgumentException("IP地址不能为空", nameof(ip));
        }

        await _refreshLock.WaitAsync();
        try
        {
            // 锁内直接获取数据，避免重入死锁
            var blacklistData = memoryCache.TryGetValue<BlacklistData>(MemoryCacheKey, out var cached) && cached != null
                ? cached
                : await LoadBlacklistDataInternalAsync();

            if (ip.Contains('/'))
            {
                var cidrRange = ParseCidr(ip);
                if (cidrRange == null)
                    throw new ArgumentException($"无效的CIDR格式: {ip}", nameof(ip));

                if (blacklistData.CidrRanges.All(r => r.Cidr != ip))
                {
                    blacklistData.CidrRanges.Add(cidrRange);
                    logger.LogInformation("添加CIDR范围到黑名单: {Cidr}", ip);
                }
            }
            else
            {
                if (blacklistData.ExactIps.Add(ip))
                    logger.LogInformation("添加IP到黑名单: {Ip}", ip);
            }

            await SaveBlacklistToRedisAsync(blacklistData);
            memoryCache.Set(MemoryCacheKey, blacklistData, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(MemoryCacheExpirationMinutes)
            });
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <summary>
    /// 从黑名单中移除IP
    /// </summary>
    public async Task RemoveFromBlacklistAsync(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            throw new ArgumentException("IP地址不能为空", nameof(ip));
        }

        await _refreshLock.WaitAsync();
        try
        {
            var blacklistData = memoryCache.TryGetValue<BlacklistData>(MemoryCacheKey, out var cached) && cached != null
                ? cached
                : await LoadBlacklistDataInternalAsync();

            if (ip.Contains('/'))
            {
                if (blacklistData.CidrRanges.RemoveAll(r => r.Cidr == ip) > 0)
                    logger.LogInformation("从黑名单移除CIDR范围: {Cidr}", ip);
            }
            else
            {
                if (blacklistData.ExactIps.Remove(ip))
                    logger.LogInformation("从黑名单移除IP: {Ip}", ip);
            }

            await SaveBlacklistToRedisAsync(blacklistData);
            memoryCache.Set(MemoryCacheKey, blacklistData, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(MemoryCacheExpirationMinutes)
            });
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <summary>
    /// 获取黑名单统计信息
    /// </summary>
    public async Task<BlacklistStats> GetStatsAsync()
    {
        var blacklistData = await GetOrLoadBlacklistAsync();

        return new BlacklistStats
        {
            TotalIps = blacklistData.ExactIps.Count,
            TotalCidrRanges = blacklistData.CidrRanges.Count,
            CacheHits = Interlocked.Read(ref _cacheHits),
            CacheMisses = Interlocked.Read(ref _cacheMisses),
            BlacklistHits = Interlocked.Read(ref _blacklistHits),
            LastRefreshTime = _lastRefreshTime
        };
    }

    #endregion
}