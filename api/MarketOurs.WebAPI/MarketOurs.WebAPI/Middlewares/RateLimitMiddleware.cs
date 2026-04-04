using System.Globalization;
using MarketOurs.DataAPI.Exceptions;
using MarketOurs.WebAPI.Controllers;
using MarketOurs.WebAPI.Services;

namespace MarketOurs.WebAPI.Middlewares;

/// <summary>
/// 请求频率限制中间件，用于防止暴力破解和恶意请求
/// </summary>
public class RateLimitMiddleware(
    RequestDelegate next,
    ILogger<RateLimitMiddleware> logger,
    RateLimitService rateLimitService,
    IIpBlacklistCacheService ipBlacklistService)
{
    // 异常请求检测阈值
    private const int SuspiciousRequestThreshold = 1000; // 每分钟超过1000个请求视为可疑

    // 可疑请求计数器（IP -> 时间戳列表）
    private readonly Dictionary<string, List<DateTime>> _suspiciousRequestTracker = new();

    public async Task InvokeAsync(HttpContext context)
    {
        // 获取客户端IP地址
        var clientIp = GetClientIp(context);

        // 1. 检查请求来源是否在黑名单中（使用Redis缓存）
        if (await ipBlacklistService.IsIpBlacklistedAsync(clientIp))
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("黑名单IP请求被拒绝：{ClientIp}", clientIp);
            }
            await ReturnRateLimitResponse(context);
            return;
        }

        // 2. 动态调整限流阈值
        rateLimitService.AdjustRateLimitsDynamically();

        // 3. 获取匹配的速率限制策略
        var path = context.Request.Path.Value ?? "";
        var policy = rateLimitService.GetMatchingPolicy(path);

        // 4. 尝试获取令牌（按IP分组）
        var lease = await rateLimitService.TryAcquireTokenAsync(policy, clientIp, CancellationToken.None);

        // 5. 记录请求统计
        rateLimitService.RecordRequest(clientIp, path, lease.IsAcquired);

        if (lease.IsAcquired)
        {
            // 6. 检查是否为异常请求
            if (IsSuspiciousRequest(clientIp))
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("可疑请求被检测到：{ClientIp}, Path: {Path}", clientIp, path);
                }
                // 可以在这里添加告警逻辑
            }

            // 如果获取到令牌，继续处理请求
            await next(context);
        }
        else
        {
            // 如果没有获取到令牌，返回429 Too Many Requests
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("请求频率限制触发，客户端IP：{ClientIp}, 策略：{PolicyName}", clientIp, policy.Name);
            }
            await ReturnRateLimitResponse(context, policy);
        }
    }

    /// <summary>
    /// 获取客户端真实IP地址
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>客户端IP地址</returns>
    private string GetClientIp(HttpContext context)
    {
        // 检查X-Forwarded-For头（处理反向代理情况）
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // 检查X-Real-IP头
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp.Trim();
        }

        // 直接从连接获取IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// 检查是否为可疑请求
    /// </summary>
    /// <param name="clientIp">客户端IP</param>
    /// <returns>是否为可疑请求</returns>
    private bool IsSuspiciousRequest(string clientIp)
    {
        // 清理旧的请求记录（只保留最近1分钟的）
        var now = DateTime.UtcNow;
        var oneMinuteAgo = now.AddMinutes(-1);

        if (!_suspiciousRequestTracker.TryGetValue(clientIp, out var requestTimes))
        {
            requestTimes = new List<DateTime>();
            _suspiciousRequestTracker[clientIp] = requestTimes;
        }

        // 清理过期记录
        requestTimes.RemoveAll(time => time < oneMinuteAgo);

        // 添加当前请求时间
        requestTimes.Add(now);

        // 检查请求频率是否超过阈值
        return requestTimes.Count > SuspiciousRequestThreshold;
    }

    /// <summary>
    /// 返回速率限制响应
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="policy">速率限制策略</param>
    private async Task ReturnRateLimitResponse(HttpContext context, RateLimitPolicy? policy = null)
    {
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        // 根据策略设置不同的Retry-After头
        var retryAfter = policy?.ReplenishmentPeriod.TotalSeconds ?? 60;
        context.Response.Headers.Append("Retry-After", retryAfter.ToString(CultureInfo.InvariantCulture));

        await context.Response.WriteAsJsonAsync(ApiResponse<string>.Fail(ErrorCode.TooManyRequests,
            "请求频率过高，请稍后再试"));
    }
}