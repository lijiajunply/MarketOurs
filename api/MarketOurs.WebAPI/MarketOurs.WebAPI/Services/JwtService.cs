using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Configs;
using MarketOurs.DataAPI.Services;
using Microsoft.IdentityModel.Tokens;

namespace MarketOurs.WebAPI.Services;

public class JwtService(
    JwtConfig jwtConfig,
    RsaKeyManager rsaKeyManager,
    ILogger<JwtService> logger) : IJwtService
{
    public Task<string> GetAccessToken(UserDto user, DeviceType deviceType)
    {
        try
        {
            var now = DateTime.UtcNow;
            var jwtId = Guid.NewGuid().ToString(); // 用于防止重放攻击

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Name),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, jwtId), // JWT ID 防止重放攻击
                new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64),
                new Claim(ClaimTypes.WindowsDeviceClaim, user.Email),
                new Claim("usage", "access") // 令牌使用场景：访问令牌
            };

            // 获取当前有效的RSA私钥
            var rsa = rsaKeyManager.GetCurrentPrivateKey();
            // 从RSA密钥中导出公钥的SHA256哈希值作为KeyId
            var publicKeyBytes = rsa.ExportRSAPublicKey();
            var keyId = Convert.ToBase64String(SHA256.HashData(publicKeyBytes)).Substring(0, 16);

            var rsaSecurityKey = new RsaSecurityKey(rsa) { KeyId = keyId };
            var signingCredentials = new SigningCredentials(
                rsaSecurityKey,
                SecurityAlgorithms.RsaSha256Signature);

            var securityToken = new JwtSecurityToken(
                issuer: jwtConfig.Issuer, // 签发者
                audience: jwtConfig.Audience, // 接收者
                claims: claims,
                notBefore: now, // 生效时间
                expires: now.AddMinutes(jwtConfig.AccessTokenExpiryMinutes), // 过期时间
                signingCredentials: signingCredentials);

            return Task.FromResult(new JwtSecurityTokenHandler().WriteToken(securityToken));
        }
        catch (Exception exception)
        {
            return Task.FromException<string>(exception);
        }
    }

    public Task<string> GetRefreshToken(DeviceType deviceType)
    {
        return Task.FromResult(Guid.NewGuid().ToString("N") + $"_{deviceType}");
    }

    public (bool isValid, IEnumerable<Claim> claims) ValidateAccessToken(string token,
        TokenValidationParameters? validationParameters = null)
    {
        try
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("开始验证访问令牌");
            }

            var tokenHandler = new JwtSecurityTokenHandler();

            // 如果没有提供验证参数，使用默认参数
            validationParameters ??= GetDefaultValidationParameters();

            // 验证令牌

            var claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out _);

            // 检查令牌使用场景
            var usageClaim = claimsPrincipal.FindFirst("usage");
            if (usageClaim is not { Value: "access" })
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("访问令牌使用场景验证失败");
                }

                return (false, []);
            }

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("访问令牌验证成功");
            }

            return (true, claimsPrincipal.Claims);
        }
        catch (SecurityTokenExpiredException)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError("访问令牌已过期");
            }

            // 重新抛出令牌过期异常，让中间件捕获处理
            throw;
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "访问令牌验证失败");
            }

            return (false, Enumerable.Empty<Claim>());
        }
    }


    /// <summary>
    /// 获取默认的令牌验证参数
    /// </summary>
    /// <returns>令牌验证参数</returns>
    public TokenValidationParameters GetDefaultValidationParameters()
    {
        // 获取当前有效的RSA公钥
        var rsa = rsaKeyManager.GetCurrentPublicKey();

        // 从RSA密钥中导出公钥的SHA256哈希值作为KeyId，与生成令牌时使用的KeyId保持一致
        var publicKeyBytes = rsa.ExportRSAPublicKey();
        var keyId = Convert.ToBase64String(SHA256.HashData(publicKeyBytes)).Substring(0, 16);
        var rsaSecurityKey = new RsaSecurityKey(rsa) { KeyId = keyId };

        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = rsaSecurityKey,
            ValidateIssuer = true,
            ValidIssuer = jwtConfig.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtConfig.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1), // 允许1分钟的时钟偏差
            RequireExpirationTime = true,
            RequireSignedTokens = true
        };
    }
}