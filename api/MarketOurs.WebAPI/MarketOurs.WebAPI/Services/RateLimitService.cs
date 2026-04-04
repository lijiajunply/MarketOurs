using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using MarketOurs.DataAPI.Configs;
using StackExchange.Redis;

namespace MarketOurs.WebAPI.Services;

/// <summary>
/// 速率限制服务
/// </summary>
public class RateLimitService : IDisposable
{
    private readonly RateLimitConfig _config;
    private readonly ILogger<RateLimitService> _logger;
    private readonly IDatabase? _redisDb;

    // 启动时预排序 + 预编译 Regex，避免每次请求重复排序和构造正则
    private readonly (RateLimitPolicy Policy, Regex? CompiledRegex)[] _sortedPolicies;

    // 路径 → 策略缓存：每条唯一路径只匹配一次
    private readonly ConcurrentDictionary<string, RateLimitPolicy> _pathPolicyCache = new(StringComparer.OrdinalIgnoreCase);

    // 本地备用限流器（Redis 不可用时）
    private readonly Dictionary<string, PartitionedRateLimiter<string>> _localLimiters = new();

    // 动态倍率：Interlocked long bits 保证原子读写，不再修改 Policy 对象
    private long _dynamicMultiplierBits = BitConverter.DoubleToInt64Bits(1.0);
    private double DynamicMultiplier
    {
        get => BitConverter.Int64BitsToDouble(Interlocked.Read(ref _dynamicMultiplierBits));
        set => Interlocked.Exchange(ref _dynamicMultiplierBits, BitConverter.DoubleToInt64Bits(value));
    }
    private readonly Timer? _adjustmentTimer;

    // CPU 采样状态（无锁滚动）
    private TimeSpan _lastCpuTime;
    private DateTime _lastSampleTime;

    // Redis Lua 脚本（固定窗口，原子操作）
    private static readonly string FixedWindowScript = @"
        local current = redis.call('INCR', KEYS[1])
        if current == 1 then
            redis.call('EXPIRE', KEYS[1], ARGV[2])
        end
        local limit = tonumber(ARGV[1])
        if current > limit then
            return {0, 0, redis.call('TTL', KEYS[1])}
        end
        return {1, limit - current, redis.call('TTL', KEYS[1])}";

    // Redis Lua 脚本（滑动窗口，有序集合，原子操作）
    private static readonly string SlidingWindowScript = @"
        local now    = tonumber(ARGV[1])
        local window = tonumber(ARGV[2]) * 1000
        local limit  = tonumber(ARGV[3])
        local expire = tonumber(ARGV[2]) + 1
        redis.call('ZREMRANGEBYSCORE', KEYS[1], '-inf', now - window)
        local count = redis.call('ZCARD', KEYS[1])
        if count >= limit then
            return {0, 0, tonumber(ARGV[2])}
        end
        redis.call('ZADD', KEYS[1], now, now .. ':' .. redis.call('INCR', KEYS[1] .. ':seq'))
        redis.call('EXPIRE', KEYS[1], expire)
        return {1, limit - count - 1, tonumber(ARGV[2])}";

    public RateLimitService(RateLimitConfig config, ILogger<RateLimitService> logger,
        IEnumerable<IConnectionMultiplexer> redisMultiplexers)
    {
        _config = config;
        _logger = logger;

        // 预排序策略并编译 Regex（只在启动时执行一次）
        _sortedPolicies = config.Policies
            .OrderBy(p => p.Priority)
            .Select(p => (p, CompilePattern(p.PathPattern)))
            .ToArray();

        foreach (var policy in config.Policies)
            _localLimiters[policy.Name] = CreateLocalLimiter(policy);

        if (config.EnableRedis)
        {
            try { _redisDb = redisMultiplexers.FirstOrDefault()?.GetDatabase(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis 连接失败，降级为本地限流。");
            }
        }

        // 后台定时采样 + 动态调整，彻底从请求热路径移除
        if (config.EnableDynamicAdjustment)
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            _lastCpuTime = process.TotalProcessorTime;
            _lastSampleTime = DateTime.UtcNow;
            _adjustmentTimer = new Timer(_ => SampleAndAdjust(), null,
                TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }
    }

    private static Regex? CompilePattern(string pattern)
    {
        if (pattern == "*") return null; // 通配符直接跳过正则
        var escaped = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return new Regex(escaped, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    public RateLimitPolicy GetMatchingPolicy(string path)
    {
        return _pathPolicyCache.GetOrAdd(path, p =>
        {
            foreach (var (policy, regex) in _sortedPolicies)
            {
                if (!policy.Enabled) continue;
                if (regex == null || regex.IsMatch(p)) return policy;
            }
            return _sortedPolicies[^1].Policy;
        });
    }

    public async Task<RateLimitResult> CheckRateLimitAsync(string path, string identifier)
    {
        if (!_config.Enabled) return new RateLimitResult(true, 1, 1, DateTimeOffset.UtcNow);

        var policy = GetMatchingPolicy(path);
        if (!policy.Enabled) return new RateLimitResult(true, 1, 1, DateTimeOffset.UtcNow);

        var effectiveLimit = Math.Max(1, (int)(policy.PermitLimit * DynamicMultiplier));

        if (_redisDb != null)
        {
            try { return await CheckRedisAsync(policy, identifier, effectiveLimit); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis 限流检查失败（{Identifier}），降级为本地。", identifier);
            }
        }

        return CheckLocal(policy, identifier, effectiveLimit);
    }

    private RateLimitResult CheckLocal(RateLimitPolicy policy, string identifier, int effectiveLimit)
    {
        var lease = _localLimiters[policy.Name].AttemptAcquire(identifier);
        return new RateLimitResult(lease.IsAcquired, 0, effectiveLimit,
            DateTimeOffset.UtcNow.Add(policy.Window));
    }

    private Task<RateLimitResult> CheckRedisAsync(RateLimitPolicy policy, string identifier, int effectiveLimit) =>
        policy.Algorithm == RateLimitAlgorithm.SlidingWindow
            ? CheckRedisSlidingWindowAsync(policy, identifier, effectiveLimit)
            : CheckRedisFixedWindowAsync(policy, identifier, effectiveLimit);

    private async Task<RateLimitResult> CheckRedisFixedWindowAsync(RateLimitPolicy policy, string identifier, int effectiveLimit)
    {
        var key = $"{_config.RedisKeyPrefix}{policy.Name}:{identifier}";
        var result = (RedisValue[]?)await _redisDb!.ScriptEvaluateAsync(FixedWindowScript,
            [(RedisKey)key],
            [effectiveLimit, (int)policy.Window.TotalSeconds]);

        if (result == null || result.Length < 3)
            return new RateLimitResult(false, 0, effectiveLimit, DateTimeOffset.UtcNow);

        return new RateLimitResult(
            (int)result[0] == 1,
            Math.Max(0, (int)result[1]),
            effectiveLimit,
            DateTimeOffset.UtcNow.AddSeconds(Math.Max(0, (long)result[2])));
    }

    private async Task<RateLimitResult> CheckRedisSlidingWindowAsync(RateLimitPolicy policy, string identifier, int effectiveLimit)
    {
        var key = $"{_config.RedisKeyPrefix}{policy.Name}:sw:{identifier}";
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var result = (RedisValue[]?)await _redisDb!.ScriptEvaluateAsync(SlidingWindowScript,
            [(RedisKey)key],
            [nowMs, (int)policy.Window.TotalSeconds, effectiveLimit]);

        if (result == null || result.Length < 3)
            return new RateLimitResult(false, 0, effectiveLimit, DateTimeOffset.UtcNow);

        return new RateLimitResult(
            (int)result[0] == 1,
            Math.Max(0, (int)result[1]),
            effectiveLimit,
            DateTimeOffset.UtcNow.AddSeconds(Math.Max(0, (long)result[2])));
    }

    private PartitionedRateLimiter<string> CreateLocalLimiter(RateLimitPolicy policy) =>
        PartitionedRateLimiter.Create<string, string>(key => policy.Algorithm switch
        {
            RateLimitAlgorithm.FixedWindow => RateLimitPartition.GetFixedWindowLimiter(key, _ =>
                new FixedWindowRateLimiterOptions
                {
                    PermitLimit = policy.PermitLimit,
                    Window = policy.Window,
                    QueueLimit = policy.QueueLimit
                }),
            RateLimitAlgorithm.SlidingWindow => RateLimitPartition.GetSlidingWindowLimiter(key, _ =>
                new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = policy.PermitLimit,
                    Window = policy.Window,
                    SegmentsPerWindow = 6,
                    QueueLimit = policy.QueueLimit
                }),
            RateLimitAlgorithm.Concurrency => RateLimitPartition.GetConcurrencyLimiter(key, _ =>
                new ConcurrencyLimiterOptions
                {
                    PermitLimit = policy.PermitLimit,
                    QueueLimit = policy.QueueLimit
                }),
            _ => RateLimitPartition.GetTokenBucketLimiter(key, _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = policy.PermitLimit,
                ReplenishmentPeriod = policy.ReplenishmentPeriod,
                TokensPerPeriod = policy.TokensPerPeriod,
                QueueLimit = policy.QueueLimit
            })
        });

    /// <summary>
    /// 后台定时 CPU 采样 + 动态限流调整（不阻塞请求线程）
    /// </summary>
    private void SampleAndAdjust()
    {
        try
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var nowCpu = process.TotalProcessorTime;
            var now = DateTime.UtcNow;

            var elapsedMs = (now - _lastSampleTime).TotalMilliseconds;
            if (elapsedMs < 100) return;

            var cpuUsed = (nowCpu - _lastCpuTime).TotalMilliseconds;
            var load = cpuUsed / (elapsedMs * Environment.ProcessorCount);

            _lastCpuTime = nowCpu;
            _lastSampleTime = now;

            if (load > _config.SystemLoadThreshold)
            {
                var reduction = Math.Min(0.5, (load - _config.SystemLoadThreshold) * 2);
                DynamicMultiplier = 1.0 - reduction;
                _logger.LogWarning("系统负载高 ({Load:P1})，限流上限降至 {Pct:P0}", load, DynamicMultiplier);
            }
            else
            {
                DynamicMultiplier = 1.0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "动态限流调整采样失败。");
        }
    }

    public void Dispose()
    {
        _adjustmentTimer?.Dispose();
        foreach (var limiter in _localLimiters.Values)
            limiter.Dispose();
    }
}
