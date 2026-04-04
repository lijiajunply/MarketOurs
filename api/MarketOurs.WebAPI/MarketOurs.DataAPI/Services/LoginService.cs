using MarketOurs.DataAPI.Configs;
using MarketOurs.DataAPI.Exceptions;
using StackExchange.Redis;

namespace MarketOurs.DataAPI.Services;

public interface ILoginService
{
    public Task<string> Login(string account, string password, string deviceType);

    /// <summary>
    /// 根据 RefToken 进行登录
    /// </summary>
    /// <param name="token"></param>
    /// <param name="deviceType"></param>
    /// <returns></returns>
    public Task<string> Login(string token, string deviceType);

    /// <summary>
    /// OAuth2 第三方登录
    /// </summary>
    public Task<string> LoginWithOAuthAsync(string email, string name, string avatar, string deviceType);

    public Task<bool> Logout(string id, string deviceType);
    public Task<bool> ValidateToken(string userId, string token, string deviceType);
}

public class LoginService(
    IUserService userService,
    IEnumerable<IConnectionMultiplexer> redisEnumerable,
    IJwtService jwtService) : ILoginService
{
    private readonly IConnectionMultiplexer? _redis = redisEnumerable.FirstOrDefault();

    public async Task<string> Login(string account, string password, string deviceType)
    {
        var user = await userService.LoginAsync(account, password);
        if (user == null)
        {
            throw new AuthException(ErrorCode.UserNotFound, "用户不存在或密码错误");
        }

        return await GenerateTokenForUser(user, deviceType);
    }

    public async Task<string> LoginWithOAuthAsync(string account, string name, string avatar, string deviceType)
    {
        var user = await userService.GetByAccountAsync(account);
        if (user == null)
        {
            // 如果不存在，则创建一个随机密码的用户，并进行加密
            var randomPassword = Guid.NewGuid().ToString("N");
            user = await userService.CreateAsync(new Data.DTOs.UserCreateDto
            {
                Account = account, // 使用统一的Account字段
                Password = randomPassword,
                Name = name,
                Role = "User"
            });
            // Update avatar if provided
            if (!string.IsNullOrEmpty(avatar))
            {
                await userService.UpdateAsync(user.Id, new Data.DTOs.UserUpdateDto
                {
                    Name = name,
                    Avatar = avatar,
                    Info = "来自第三方登录"
                });
            }
        }
        else if (!user.IsActive)
        {
            throw new AuthException(ErrorCode.UserLocked, "您的账号已被锁定或禁用");
        }

        return await GenerateTokenForUser(user, deviceType);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="user"></param>
    /// <param name="deviceType"></param>
    /// <returns></returns>
    private async Task<string> GenerateTokenForUser(Data.DTOs.UserDto user, string deviceType)
    {
        // 无论如何，每次登录都签发一个具有完整有效期的新 Token
        var token = await jwtService.GetAccessToken(user, deviceType.GetDeviceTypeEnum());

        var db = _redis?.GetDatabase();
        if (db == null) return token;
        var key = CacheKeys.UserAccessToken(user.Id, deviceType);
        var refreshToken = await jwtService.GetRefreshToken(deviceType.GetDeviceTypeEnum());

        // 使用异步方法将新 Token 存入 Redis，直接覆盖旧值 (可用于踢掉旧设备的会话)
        await db.StringSetAsync(key, token, TimeSpan.FromHours(2));
        await db.StringSetAsync(CacheKeys.UserRefreshToken(refreshToken), user.Id, TimeSpan.FromDays(3));

        return token;
    }

    public async Task<string> Login(string token, string deviceType)
    {
        var db = _redis?.GetDatabase();

        if (db == null) throw new BusinessException(ErrorCode.CacheOperationFailed, "Redis服务未找到");
        var id = "";
        var idRedis = await db.StringGetAsync(CacheKeys.UserRefreshToken(token));
        if (idRedis.HasValue)
        {
            id = idRedis;
        }

        if (string.IsNullOrEmpty(id)) return "";
        var key = CacheKeys.UserAccessToken(id, deviceType);
        var accessToken = await db.StringGetAsync(key);
        return accessToken.HasValue ? accessToken.ToString() : "";
    }

    public async Task<bool> Logout(string id, string deviceType)
    {
        var db = _redis?.GetDatabase();

        if (db == null) return true;

        if (string.IsNullOrEmpty(deviceType)) deviceType = "*";

        var key = CacheKeys.UserAccessToken(id, deviceType);

        return await db.KeyDeleteAsync(key);
    }

    public async Task<bool> ValidateToken(string userId, string token, string deviceType)
    {
        var db = _redis?.GetDatabase();

        if (db == null) return jwtService.ValidateAccessToken(token).isValid;

        var t = await db.StringGetAsync(CacheKeys.UserAccessToken(userId, deviceType));

        if (!t.HasValue) return false;

        return t.ToString() == token;
    }
}