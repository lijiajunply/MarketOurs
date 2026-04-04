using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Services;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MarketOurs.WebAPI.IdentityModels;

public class GlobalAuthorizationFilter(
    ILoginService loginService,
    IJwtService jwtService) : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // 仅在请求的动作或控制器标注了 [Authorize] 且未标注 [AllowAnonymous] 时才执行校验
        var endpointMetadata = context.ActionDescriptor.EndpointMetadata;
        var hasAuthorize = endpointMetadata?.Any(m =>
            m is Microsoft.AspNetCore.Authorization.AuthorizeAttribute
                or Microsoft.AspNetCore.Authorization.IAuthorizeData) == true;
        var hasAllowAnonymous =
            endpointMetadata?.Any(m => m is Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute) == true;

        if (!hasAuthorize || hasAllowAnonymous)
        {
            return;
        }

        var bearer = context.HttpContext.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(bearer) || !bearer.StartsWith("Bearer "))
        {
            return;
        }

        try
        {
            var jwtParts = bearer.Split(' ', 2);
            if (jwtParts.Length != 2)
            {
                return;
            }

            var token = jwtParts[1];

            // 使用JwtService进行token验证，确保使用相同的密钥和验证逻辑
            var (isValid, claims) = jwtService.ValidateAccessToken(token);

            if (!isValid)
            {
                return;
            }

            // 验证token是否在Redis中存在（防止已注销的token继续使用）
            var enumerable = claims as Claim[] ?? claims.ToArray();
            var userId = enumerable.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value ??
                         enumerable.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var deviceType = enumerable.FirstOrDefault(c => c.Type == ClaimTypes.WindowsDeviceClaim)?.Value;

            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(deviceType))
            {
                var isValidInRedis = loginService.ValidateToken(userId, token, deviceType).Result;
                if (!isValidInRedis)
                {
                    return;
                }
            }

            // 创建ClaimsPrincipal
            var claimsIdentity = new ClaimsIdentity(enumerable, "JWT");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            // 设置用户上下文
            context.HttpContext.User = claimsPrincipal;
        }
        catch (Exception ex)
        {
            // 记录异常但不中断流程
            Console.WriteLine($"Token validation error: {ex.Message}");
        }
    }
}

public static class TokenHelper
{
    public static UserDto? GetUser(this ClaimsPrincipal? claimsPrincipal)
    {
        var claimId = claimsPrincipal?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        var claimRole = claimsPrincipal?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
        if (claimId.IsNull() || claimRole.IsNull())
        {
            return null;
        }

        return new UserDto()
        {
            Id = claimId!.Value,
            Role = claimRole!.Value,
        };
    }

    public static string? GetJwt(this HttpContext context)
    {
        var bearer = context.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(bearer) || !bearer.Contains("Bearer")) return null;
        var jwt = bearer.Split(' ');
        return jwt.Length != 2 ? null : jwt[1];
    }

    private static bool IsNull(this Claim? claim)
        => claim == null || string.IsNullOrEmpty(claim.Value);
}