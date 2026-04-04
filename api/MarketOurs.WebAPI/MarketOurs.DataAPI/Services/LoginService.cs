using MarketOurs.DataAPI.Exceptions;
using StackExchange.Redis;

namespace MarketOurs.DataAPI.Services;

public interface ILoginService
{
    public Task<string> Login(string email, string password, string deviceType);

    /// <summary>
    /// 根据 RefToken 进行登录
    /// </summary>
    /// <param name="token"></param>
    /// <param name="deviceType"></param>
    /// <returns></returns>
    public Task<string> Login(string token, string deviceType);

    public Task<bool> Logout(string id, string deviceType);
    public Task<bool> ValidateToken(string userId, string token, string deviceType);
}

public class LoginService(
    IUserService userService,
    IEnumerable<IConnectionMultiplexer> redisEnumerable,
    IJwtService jwtService) : ILoginService
{
    private readonly IConnectionMultiplexer? _redis = redisEnumerable.FirstOrDefault();

    public async Task<string> Login(string email, string password, string deviceType)
    {
        var user = await userService.LoginAsync(email, password);
        if (user == null)
        {
            throw new AuthException(ErrorCode.UserNotFound, "用户不存在或密码错误");
        }

        // 无论如何，每次登录都签发一个具有完整有效期的新 Token
        var token = await jwtService.GetAccessToken(user, deviceType.GetDeviceTypeEnum());

        var db = _redis?.GetDatabase();
        if (db == null) return token;
        var key = $"access_token:{user.Id}_{deviceType}";
        var refreshToken = await jwtService.GetRefreshToken(deviceType.GetDeviceTypeEnum());

        // 使用异步方法将新 Token 存入 Redis，直接覆盖旧值 (可用于踢掉旧设备的会话)
        await db.StringSetAsync(key, token, TimeSpan.FromHours(2));
        await db.StringSetAsync(refreshToken, user.Id, TimeSpan.FromDays(3));

        return token;
    }

    public async Task<string> Login(string token, string deviceType)
    {
        var db = _redis?.GetDatabase();

        if (db == null) throw new BusinessException(ErrorCode.CacheOperationFailed, "Redis服务未找到");
        var id = "";
        var idRedis = await db.StringGetAsync(token);
        if (idRedis.HasValue)
        {
            id = idRedis;
        }

        if (string.IsNullOrEmpty(id)) return "";
        var key = $"access_token:{id}_{deviceType}";
        var accessToken = await db.StringGetAsync(key);
        return accessToken.HasValue ? accessToken.ToString() : "";
    }

    public async Task<bool> Logout(string id, string deviceType)
    {
        var db = _redis?.GetDatabase();

        if (db == null) return true;

        if (string.IsNullOrEmpty(deviceType)) deviceType = "*";

        var key = $"access_token:{id}_{deviceType}";

        return await db.KeyDeleteAsync(key);
    }

    public async Task<bool> ValidateToken(string userId, string token, string deviceType)
    {
        var db = _redis?.GetDatabase();

        if (db == null) return jwtService.ValidateAccessToken(token).isValid;

        var t = await db.StringGetAsync($"access_token:{userId}_{deviceType}");

        if (!t.HasValue) return false;

        return t.ToString() == token;
    }
}