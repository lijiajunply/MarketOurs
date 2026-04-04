using System.Threading.RateLimiting;

namespace MarketOurs.WebAPI.Services;

/// <summary>
/// API速率限制策略
/// </summary>
public class RateLimitPolicy
{
    /// <summary>
    /// 策略名称
    /// </summary>
    public string Name { get; set; } = "default";

    /// <summary>
    /// 匹配的API路径模式（支持通配符*）
    /// </summary>
    public string PathPattern { get; set; } = "*";

    /// <summary>
    /// 令牌桶容量
    /// </summary>
    public int TokenLimit { get; set; } = 100;

    /// <summary>
    /// 令牌补充周期
    /// </summary>
    public TimeSpan ReplenishmentPeriod { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// 每个周期补充的令牌数
    /// </summary>
    public int TokensPerPeriod { get; set; } = 100;

    /// <summary>
    /// 是否自动补充令牌
    /// </summary>
    public bool AutoReplenishment { get; set; } = true;

    /// <summary>
    /// 是否启用该策略
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 限流策略优先级（数值越小优先级越高）
    /// </summary>
    public int Priority { get; set; } = 100;
}

/// <summary>
/// 速率限制配置
/// </summary>
public class RateLimitConfig
{
    /// <summary>
    /// 是否启用速率限制
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 是否启用动态限流调整
    /// </summary>
    public bool EnableDynamicAdjustment { get; set; } = true;

    /// <summary>
    /// 动态调整间隔
    /// </summary>
    public TimeSpan DynamicAdjustmentInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 系统负载阈值（超过该阈值将触发动态限流调整）
    /// </summary>
    public double SystemLoadThreshold { get; set; } = 0.8;

    /// <summary>
    /// 限流策略列表
    /// </summary>
    public List<RateLimitPolicy> Policies { get; set; } =
    [
        new()
        {
            Name = "default",
            PathPattern = "*",
            TokenLimit = 200, // 从100提升到200，每个IP每分钟200次请求
            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
            TokensPerPeriod = 200,
            AutoReplenishment = true,
            Enabled = true,
            Priority = 100
        },
        // 登录接口策略（防止暴力破解，但不影响正常用户）

        new()
        {
            Name = "login",
            PathPattern = "/auth/*",
            TokenLimit = 30, // 从10提升到30，每个IP每分钟30次登录尝试
            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
            TokensPerPeriod = 30,
            AutoReplenishment = true,
            Enabled = true,
            Priority = 10
        },
        // 注册接口策略（防止批量注册）

        new()
        {
            Name = "register",
            PathPattern = "/users/register",
            TokenLimit = 10, // 从5提升到10，每个IP每5分钟10次注册
            ReplenishmentPeriod = TimeSpan.FromMinutes(5),
            TokensPerPeriod = 10,
            AutoReplenishment = true,
            Enabled = true,
            Priority = 10
        },
        // 敏感操作接口策略

        new()
        {
            Name = "sensitive",
            PathPattern = "/admin/*",
            TokenLimit = 100, // 从50提升到100，每个IP每分钟100次管理操作
            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
            TokensPerPeriod = 100,
            AutoReplenishment = true,
            Enabled = true,
            Priority = 50
        }
    ];
}

/// <summary>
/// 速率限制状态
/// </summary>
[Serializable]
public class RateLimitState
{
    /// <summary>
    /// 当前系统负载
    /// </summary>
    public double CurrentSystemLoad { get; set; }

    /// <summary>
    /// 最后调整时间
    /// </summary>
    public DateTime LastAdjustmentTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 当前激活的策略
    /// </summary>
    public Dictionary<string, RateLimitPolicy> ActivePolicies { get; set; } = new();
}

/// <summary>
/// IP限流器包装类
/// </summary>
public class IpRateLimiterGroup(RateLimitPolicy policy)
{
    private readonly Dictionary<string, TokenBucketRateLimiter> _limiters = new();
    private readonly Lock _lock = new();

    public TokenBucketRateLimiter GetOrCreateLimiter(string ipAddress)
    {
        lock (_lock)
        {
            if (_limiters.TryGetValue(ipAddress, out var limiter))
            {
                return limiter;
            }

            // 为该IP创建新的限流器
            var newLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = policy.TokenLimit,
                ReplenishmentPeriod = policy.ReplenishmentPeriod,
                TokensPerPeriod = policy.TokensPerPeriod,
                AutoReplenishment = policy.AutoReplenishment
            });

            _limiters[ipAddress] = newLimiter;

            // 清理过期的限流器（保持字典大小在合理范围内）
            if (_limiters.Count > 10000)
            {
                CleanupOldLimiters();
            }

            return newLimiter;
        }
    }

    private void CleanupOldLimiters()
    {
        // 简单策略：移除一半的限流器（可以改进为基于LRU）
        var toRemove = _limiters.Take(_limiters.Count / 2).ToList();
        foreach (var kvp in toRemove)
        {
            kvp.Value.Dispose();
            _limiters.Remove(kvp.Key);
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var limiter in _limiters.Values)
            {
                limiter.Dispose();
            }

            _limiters.Clear();
        }
    }
}

/// <summary>
/// 速率限制服务
/// </summary>
public class RateLimitService : IDisposable
{
    private readonly RateLimitConfig _config;
    private readonly RateLimitState _state;
    private readonly ILogger<RateLimitService> _logger;

    // 保存原始配置，用于动态调整时计算
    private readonly Dictionary<string, RateLimitPolicy> _originalPolicies = new();

    // 按策略分组的IP限流器
    private readonly Dictionary<string, IpRateLimiterGroup> _limiterGroups = new();

    public RateLimitService(RateLimitConfig config, ILogger<RateLimitService> logger)
    {
        _config = config;
        _logger = logger;
        _state = new RateLimitState();

        // 保存原始配置
        foreach (var policy in _config.Policies)
        {
            _originalPolicies[policy.Name] = new RateLimitPolicy
            {
                Name = policy.Name,
                PathPattern = policy.PathPattern,
                TokenLimit = policy.TokenLimit,
                ReplenishmentPeriod = policy.ReplenishmentPeriod,
                TokensPerPeriod = policy.TokensPerPeriod,
                AutoReplenishment = policy.AutoReplenishment,
                Enabled = policy.Enabled,
                Priority = policy.Priority
            };
        }

        // 初始化限流器组
        InitializeLimiterGroups();
    }

    /// <summary>
    /// 初始化限流器组
    /// </summary>
    private void InitializeLimiterGroups()
    {
        foreach (var policy in _config.Policies.OrderBy(p => p.Priority))
        {
            if (policy.Enabled && !_limiterGroups.ContainsKey(policy.Name))
            {
                _limiterGroups.Add(policy.Name, new IpRateLimiterGroup(policy));
                _state.ActivePolicies[policy.Name] = policy;
            }
        }
    }

    /// <summary>
    /// 获取匹配的速率限制策略
    /// </summary>
    /// <param name="path">API路径</param>
    /// <returns>匹配的策略</returns>
    public RateLimitPolicy GetMatchingPolicy(string path)
    {
        // 按照优先级排序，优先匹配优先级高的策略
        foreach (var policy in _config.Policies.OrderBy(p => p.Priority))
        {
            if (policy.Enabled && IsPathMatch(policy.PathPattern, path))
            {
                return policy;
            }
        }

        // 默认返回默认策略
        return _config.Policies.FirstOrDefault(p => p.Name == "default") ?? _config.Policies[0];
    }

    /// <summary>
    /// 检查路径是否匹配模式
    /// </summary>
    /// <param name="pattern">路径模式</param>
    /// <param name="path">API路径</param>
    /// <returns>是否匹配</returns>
    private bool IsPathMatch(string pattern, string path)
    {
        // 简单的通配符匹配实现
        var regexPattern = pattern.Replace("*", ".*");
        return System.Text.RegularExpressions.Regex.IsMatch(path, regexPattern,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// 尝试获取令牌
    /// </summary>
    /// <param name="policy">速率限制策略</param>
    /// <param name="ipAddress">客户端IP地址</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>令牌租约</returns>
    public async ValueTask<RateLimitLease> TryAcquireTokenAsync(RateLimitPolicy policy, string ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (!_limiterGroups.TryGetValue(policy.Name, out var limiterGroup))
        {
            // 如果限流器组不存在，创建一个
            limiterGroup = new IpRateLimiterGroup(policy);
            _limiterGroups[policy.Name] = limiterGroup;
        }

        // 获取该IP的限流器
        var limiter = limiterGroup.GetOrCreateLimiter(ipAddress);
        return await limiter.AcquireAsync(1, cancellationToken);
    }

    /// <summary>
    /// 动态调整限流阈值
    /// </summary>
    public void AdjustRateLimitsDynamically()
    {
        if (!_config.EnableDynamicAdjustment)
            return;

        // 检查是否需要调整（根据时间间隔）
        var timeSinceLastAdjustment = DateTime.UtcNow - _state.LastAdjustmentTime;
        if (timeSinceLastAdjustment < _config.DynamicAdjustmentInterval)
            return;

        try
        {
            // 获取当前系统负载
            var currentLoad = GetCurrentSystemLoad();
            _state.CurrentSystemLoad = currentLoad;

            // 只有在高负载时才调整
            if (currentLoad < _config.SystemLoadThreshold)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("系统负载 {CurrentLoad:P2} 低于阈值 {Threshold:P2}，跳过动态调整",
                        currentLoad, _config.SystemLoadThreshold);
                }

                _state.LastAdjustmentTime = DateTime.UtcNow;
                return;
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("动态调整限流阈值，当前系统负载: {CurrentLoad:P2}", currentLoad);
            }

            // 根据系统负载调整限流阈值（基于原始配置）
            foreach (var policyName in _originalPolicies.Keys)
            {
                if (!_originalPolicies.TryGetValue(policyName, out var originalPolicy))
                    continue;

                if (!originalPolicy.Enabled)
                    continue;

                // 基于原始配置计算新的令牌限制
                var loadFactor = Math.Min(currentLoad - _config.SystemLoadThreshold, 0.2) / 0.2; // 0-1之间
                var newTokenLimit = (int)(originalPolicy.TokenLimit * (1 - loadFactor * 0.3)); // 最多降低30%
                newTokenLimit = Math.Max(newTokenLimit, originalPolicy.TokenLimit / 2); // 确保不低于原限制的一半
                newTokenLimit = Math.Max(newTokenLimit, 5); // 确保不低于5

                // 更新当前策略配置（不修改原始配置）
                var currentPolicy = _config.Policies.FirstOrDefault(p => p.Name == policyName);
                if (currentPolicy != null)
                {
                    currentPolicy.TokenLimit = newTokenLimit;
                    currentPolicy.TokensPerPeriod = newTokenLimit;
                }

                // 重新创建限流器组（清空现有的IP限流器）
                if (_limiterGroups.TryGetValue(policyName, out var existingGroup))
                {
                    existingGroup.Dispose();
                    _limiterGroups.Remove(policyName);
                }

                var updatedPolicy = currentPolicy ?? originalPolicy;
                _limiterGroups[policyName] = new IpRateLimiterGroup(updatedPolicy);
                _state.ActivePolicies[policyName] = updatedPolicy;

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("更新策略 {PolicyName} 的限流阈值：{OriginalLimit} -> {NewLimit}",
                        policyName, originalPolicy.TokenLimit, newTokenLimit);
                }
            }

            // 更新最后调整时间
            _state.LastAdjustmentTime = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "动态调整限流阈值失败");
        }
    }

    /// <summary>
    /// 获取当前系统负载（模拟实现）
    /// </summary>
    /// <returns>系统负载（0-1之间）</returns>
    private double GetCurrentSystemLoad()
    {
        try
        {
#if WINDOWS
            // 对于Windows，可以使用PerformanceCounter
            using var cpuCounter = new System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuCounter.NextValue(); // First call always returns 0, so we ignore it
            System.Threading.Thread.Sleep(100); // Wait a bit before taking the actual measurement
            return cpuCounter.NextValue() / 100.0; // Convert percentage to ratio
#elif LINUX
            // 对于Linux，可以读取/proc/loadavg
            if (System.IO.File.Exists("/proc/loadavg"))
            {
                var loadAvgContent = System.IO.File.ReadAllText("/proc/loadavg");
                var parts = loadAvgContent.Split(' ');
                if (parts.Length > 0 && double.TryParse(parts[0], out var loadAvg))
                {
                    // Load average is relative to the number of CPU cores
                    var cpuCount = Environment.ProcessorCount;
                    return Math.Min(loadAvg / cpuCount, 1.0); // Normalize and cap at 1.0
                }
            }
#endif
            // 可以从监控系统获取
            // Fallback to a rough estimation based on current process
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var startTime = process.StartTime;
            var currentTime = DateTime.Now;
            var totalTime = process.TotalProcessorTime;
            var elapsedTime = currentTime - startTime;

            // Calculate CPU usage percentage
            var cpuUsage = (totalTime.TotalMilliseconds / elapsedTime.TotalMilliseconds) / Environment.ProcessorCount;
            return Math.Max(0.0, Math.Min(1.0, cpuUsage)); // Ensure value is between 0 and 1
        }
        catch
        {
            // If any error occurs, fall back to random value to avoid service disruption
            var random = new Random();
            return random.NextDouble() * 0.5; // Generate 0-0.5 between random load
        }
    }

    /// <summary>
    /// 记录请求统计信息
    /// </summary>
    /// <param name="clientIp">客户端IP</param>
    /// <param name="path">请求路径</param>
    /// <param name="isAllowed">是否允许请求</param>
    public void RecordRequest(string clientIp, string path, bool isAllowed)
    {
        // 这里可以实现请求统计逻辑，例如：
        // 1. 记录到数据库
        // 2. 记录到Redis用于实时监控
        // 3. 用于异常请求检测

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("请求统计: IP={ClientIp}, Path={Path}, Allowed={IsAllowed}", clientIp, path, isAllowed);
        }
    }

    public void Dispose()
    {
        foreach (var limiterGroup in _limiterGroups.Values)
        {
            limiterGroup.Dispose();
        }

        _limiterGroups.Clear();
    }
}