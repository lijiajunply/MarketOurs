using System.Globalization;
using MarketOurs.DataAPI.Exceptions;
using MarketOurs.WebAPI.Controllers;
using MarketOurs.WebAPI.Services;

namespace MarketOurs.WebAPI.Middlewares;

/// <summary>
/// 增强版请求频率限制中间件
/// </summary>
public class RateLimitMiddleware(
    RequestDelegate next,
    ILogger<RateLimitMiddleware> logger,
    RateLimitService rateLimitService,
    IIpBlacklistCacheService ipBlacklistService)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // 1. 获取客户端标识 (IP 或 UserID)
        var clientIp = GetClientIp(context);
        var identifier = clientIp;
        
        // 如果已登录，尝试使用 UserID 作为标识（如果策略允许）
        var path = context.Request.Path.Value ?? "/";
        var policy = rateLimitService.GetMatchingPolicy(path);
        
        if (policy.UseUserIdIfAuthenticated && context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                identifier = $"user:{userId}";
            }
        }

        // 2. 黑名单检查
        if (await ipBlacklistService.IsIpBlacklistedAsync(clientIp))
        {
            logger.LogWarning("Blocked request from blacklisted IP: {ClientIp}", clientIp);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            return;
        }

        // 3. 执行限流检查
        var result = await rateLimitService.CheckRateLimitAsync(path, identifier);

        // 5. 设置响应头 (符合 RFC 标准和行业惯例)
        context.Response.Headers["X-RateLimit-Limit"] = result.Limit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = result.Remaining.ToString();
        context.Response.Headers["X-RateLimit-Reset"] = result.ResetTime.ToUnixTimeSeconds().ToString();

        if (result.IsAllowed)
        {
            await next(context);
        }
        else
        {
            logger.LogWarning("Rate limit exceeded for {Identifier} on {Path}. Strategy: {PolicyName}", 
                identifier, path, policy.Name);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = (result.ResetTime - DateTimeOffset.Now).TotalSeconds.ToString("0", CultureInfo.InvariantCulture);

            await context.Response.WriteAsJsonAsync(ApiResponse<string>.Fail(ErrorCode.TooManyRequests,
                "请求频率过高，请稍后再试"));
        }
    }

    private string GetClientIp(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp.Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}