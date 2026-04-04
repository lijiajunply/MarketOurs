namespace MarketOurs.DataAPI.Configs;

/// <summary>
/// 限流算法
/// </summary>
public enum RateLimitAlgorithm
{
    FixedWindow, // 固定窗口
    SlidingWindow, // 滑动窗口
    TokenBucket, // 令牌桶
    Concurrency // 并发限制
}

/// <summary>
/// API速率限制策略
/// </summary>
public class RateLimitPolicy
{
    public string Name { get; set; } = "default";
    public string PathPattern { get; set; } = "*";
    public RateLimitAlgorithm Algorithm { get; set; } = RateLimitAlgorithm.TokenBucket;

    // 通用限制参数
    public int PermitLimit { get; set; } = 100;
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);

    // 令牌桶专用
    public int TokensPerPeriod { get; set; } = 100;
    public TimeSpan ReplenishmentPeriod { get; set; } = TimeSpan.FromMinutes(1);

    // 并发限制专用
    public int QueueLimit { get; set; } = 0;

    public bool Enabled { get; set; } = true;
    public int Priority { get; set; } = 100;

    // 是否按用户ID限流（如果已登录）
    public bool UseUserIdIfAuthenticated { get; set; } = true;
}

/// <summary>
/// 速率限制配置
/// </summary>
public class RateLimitConfig
{
    public bool Enabled { get; set; } = true;
    public bool EnableRedis { get; set; } = true;
    public string RedisKeyPrefix { get; set; } = "ratelimit:";

    public bool EnableDynamicAdjustment { get; set; } = true;
    public TimeSpan DynamicAdjustmentInterval { get; set; } = TimeSpan.FromMinutes(5);
    public double SystemLoadThreshold { get; set; } = 0.8;

    public List<RateLimitPolicy> Policies { get; set; } =
    [
        // 全局默认：令牌桶算法。允许适度突发，平滑限流（最大突发100，每10秒恢复20，稳态120/分）
        new()
        {
            Name = "default",
            PathPattern = "*",
            Algorithm = RateLimitAlgorithm.TokenBucket,
            PermitLimit = 100,
            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
            TokensPerPeriod = 20,
            Priority = 100
        },
        // 登录认证：滑动窗口。严格限制防暴力破解（每分钟30次）
        new()
        {
            Name = "auth",
            PathPattern = "/auth/*",
            Algorithm = RateLimitAlgorithm.SlidingWindow,
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            Priority = 10
        },
        // 账号注册：固定窗口。防止机器批量恶意注册（每4分钟10次）
        new()
        {
            Name = "register",
            PathPattern = "/auth/register",
            Algorithm = RateLimitAlgorithm.FixedWindow,
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(4),
            Priority = 10
        },
        // 文件上传：防止恶意消耗服务器带宽和I/O（每分钟10次）
        new()
        {
            Name = "upload",
            PathPattern = "/file/*",
            Algorithm = RateLimitAlgorithm.SlidingWindow,
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            Priority = 20
        },
        // 管理后台：管理员或内部调用通常需要较高的操作配额（每分钟300次）
        new()
        {
            Name = "admin",
            PathPattern = "/admin/*",
            Algorithm = RateLimitAlgorithm.SlidingWindow,
            PermitLimit = 300,
            Window = TimeSpan.FromMinutes(1),
            Priority = 50
        }
    ];
}

/// <summary>
/// 限流检查结果
/// </summary>
public record RateLimitResult(bool IsAllowed, int Remaining, int Limit, DateTimeOffset ResetTime);