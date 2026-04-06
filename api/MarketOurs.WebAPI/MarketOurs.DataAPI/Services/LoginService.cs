using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Configs;
using MarketOurs.DataAPI.Exceptions;
using StackExchange.Redis;

namespace MarketOurs.DataAPI.Services;

/// <summary>
/// 登录与会话管理服务接口，处理不同类型的登录流程及 Token 生命周期
/// </summary>
public interface ILoginService
{
    /// <summary>
    /// 标准账号密码登录
    /// </summary>
    /// <param name="account">账号 (邮箱或手机号)</param>
    /// <param name="password">密码</param>
    /// <param name="deviceType">设备类型 (Web/Mobile)</param>
    /// <returns>包含双 Token 的对象</returns>
    public Task<TokenDto> Login(string account, string password, string deviceType);

    /// <summary>
    /// 使用刷新令牌 (Refresh Token) 换取新的访问令牌
    /// </summary>
    /// <param name="token">刷新令牌</param>
    /// <param name="deviceType">设备类型</param>
    /// <returns>新签发的令牌对</returns>
    public Task<TokenDto> Login(string token, string deviceType);

    /// <summary>
    /// OAuth2 第三方登录回调处理
    /// </summary>
    /// <param name="account">第三方返回的唯一标识/邮箱</param>
    /// <param name="name">用户名</param>
    /// <param name="avatar">头像地址</param>
    /// <param name="deviceType">设备类型</param>
    /// <returns>登录成功的 Token</returns>
    public Task<TokenDto> LoginWithOAuthAsync(string account, string name, string avatar, string deviceType);

    /// <summary>
    /// 注销登录，销毁会话缓存
    /// </summary>
    /// <param name="id">用户 ID</param>
    /// <param name="deviceType">设备类型</param>
    public Task<bool> Logout(string id, string deviceType);

    /// <summary>
    /// 验证当前请求持有的 Token 是否合法且在有效期内 (与 Redis 状态一致)
    /// </summary>
    public Task<bool> ValidateToken(string userId, string token, string deviceType);
}

public class LoginService(
    IUserService userService,
    IEnumerable<IConnectionMultiplexer> redisEnumerable,
    IJwtService jwtService) : ILoginService
{
    private readonly IConnectionMultiplexer? _redis = redisEnumerable.FirstOrDefault();

    public async Task<TokenDto> Login(string account, string password, string deviceType)
    {
        var user = await userService.LoginAsync(account, password);
        if (user == null)
        {
            throw new AuthException(ErrorCode.UserNotFound, "用户不存在或密码错误");
        }

        return await GenerateTokenForUser(user, deviceType);
    }

    public async Task<TokenDto> LoginWithOAuthAsync(string account, string name, string avatar, string deviceType)
    {
        var user = await userService.GetByAccountAsync(account);
        if (user == null)
        {
            // 如果不存在，则创建一个随机密码的用户，并进行加密
            var randomPassword = Guid.NewGuid().ToString("N");
            user = await userService.CreateAsync(new UserCreateDto
            {
                Account = account, // 使用统一的Account字段
                Password = randomPassword,
                Name = name,
                Role = "User"
            });
            // Update avatar if provided
            if (!string.IsNullOrEmpty(avatar))
            {
                await userService.UpdateAsync(user.Id, new UserUpdateDto
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
    private async Task<TokenDto> GenerateTokenForUser(UserDto user, string deviceType)
    {
        // 无论如何，每次登录都签发一个具有完整有效期的新 Token
        var token = await jwtService.GetAccessToken(user, deviceType.GetDeviceTypeEnum());

        var db = _redis?.GetDatabase();
        var refreshToken = await jwtService.GetRefreshToken(deviceType.GetDeviceTypeEnum());
        if (db == null)
        {
            return new TokenDto
            {
                AccessToken = token,
                RefreshToken = refreshToken
            };
        }

        var key = CacheKeys.UserAccessToken(user.Id, deviceType);

        // 使用异步方法将新 Token 存入 Redis，直接覆盖旧值 (可用于踢掉旧设备的会话)
        await db.StringSetAsync(key, token, TimeSpan.FromHours(2));
        await db.StringSetAsync(CacheKeys.UserRefreshToken(refreshToken), user.Id, TimeSpan.FromDays(3));

        return new TokenDto
        {
            AccessToken = token,
            RefreshToken = refreshToken
        };
    }

    public async Task<TokenDto> Login(string token, string deviceType)
    {
        var db = _redis?.GetDatabase();

        if (db == null) throw new BusinessException(ErrorCode.CacheOperationFailed, "Redis服务未找到");
        var id = "";
        var idRedis = await db.StringGetAsync(CacheKeys.UserRefreshToken(token));
        if (idRedis.HasValue)
        {
            id = idRedis;
        }

        if (string.IsNullOrEmpty(id)) return new TokenDto();

        var user = await userService.GetByIdAsync(id);
        if (user is not { IsActive: true })
        {
            return new TokenDto();
        }

        // 刷新 Token 时重新生成一对令牌
        return await GenerateTokenForUser(user, deviceType);
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