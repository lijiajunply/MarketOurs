using System.Text.Json;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Configs;
using MarketOurs.DataAPI.Exceptions;
using Microsoft.Extensions.Logging;
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

    /// <summary>
    /// 预注册：检查账号是否可用并返回注册令牌
    /// </summary>
    public Task<string> RegisterUserAsync(UserCreateDto request);

    /// <summary>
    /// 发送注册验证码 (基于注册令牌)
    /// </summary>
    public Task<bool> SendRegistrationCodeAsync(string regToken);

    /// <summary>
    /// 验证并最终完成注册 (创建用户)
    /// </summary>
    public Task<UserDto> VerifyAndRegisterAsync(string regToken, string code);

    /// <summary>
    /// 发送登录验证码
    /// </summary>
    /// <param name="account">邮箱或手机号</param>
    /// <returns>是否成功</returns>
    public Task<bool> SendLoginCodeAsync(string account);

    /// <summary>
    /// 使用验证码登录 (自动注册)
    /// </summary>
    /// <param name="account">邮箱或手机号</param>
    /// <param name="code">验证码</param>
    /// <param name="deviceType">设备类型</param>
    /// <returns>令牌对</returns>
    public Task<TokenDto> LoginByCodeAsync(string account, string code, string deviceType);
}

public class LoginService(
    IUserService userService,
    IEnumerable<IConnectionMultiplexer> redisEnumerable,
    IJwtService jwtService,
    IEmailService emailService,
    ISmsService smsService,
    SmsConfig smsConfig,
    ILogger<LoginService> logger) : ILoginService
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

    /// <inheritdoc/>
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
            throw new AuthException(ErrorCode.UserNotActive, "您的账号尚未激活或已被禁用");
        }

        return await GenerateTokenForUser(user, deviceType);
    }

    /// <summary>
    /// 生成 Token
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

    /// <inheritdoc/>
    public async Task<bool> Logout(string id, string deviceType)
    {
        var db = _redis?.GetDatabase();

        if (db == null) return true;

        if (string.IsNullOrEmpty(deviceType)) deviceType = "*";

        var key = CacheKeys.UserAccessToken(id, deviceType);

        return await db.KeyDeleteAsync(key);
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateToken(string userId, string token, string deviceType)
    {
        var db = _redis?.GetDatabase();

        if (db == null) return jwtService.ValidateAccessToken(token).isValid;

        var t = await db.StringGetAsync(CacheKeys.UserAccessToken(userId, deviceType));

        if (!t.HasValue) return false;

        return t.ToString() == token;
    }

    /// <inheritdoc/>
    public async Task<string> RegisterUserAsync(UserCreateDto request)
    {
        // 1. 检查账号是否已存在
        var existingUser = await userService.GetByAccountAsync(request.Account);
        if (existingUser != null)
        {
            throw new AuthException(ErrorCode.ResourceAlreadyExists, "账号已存在");
        }

        // 2. 生成预注册令牌
        var regToken = Guid.NewGuid().ToString("N");

        if (_redis != null)
        {
            var db = _redis.GetDatabase();
            var json = JsonSerializer.Serialize(request);
            // 存入 Redis，有效期 30 分钟
            await db.StringSetAsync(CacheKeys.PreRegisterData(regToken), json, TimeSpan.FromMinutes(30));
        }
        else
        {
            throw new BusinessException(ErrorCode.CacheOperationFailed, "Redis 服务不可用");
        }

        return regToken;
    }

    /// <inheritdoc/>
    public async Task<bool> SendRegistrationCodeAsync(string regToken)
    {
        if (_redis == null) return false;
        var db = _redis.GetDatabase();

        // 1. 从 Redis 获取预注册数据
        var json = await db.StringGetAsync(CacheKeys.PreRegisterData(regToken));
        if (!json.HasValue)
        {
            throw new AuthException(ErrorCode.InvalidToken, "注册会话已过期，请重新开始");
        }

        var request = JsonSerializer.Deserialize<UserCreateDto>(json.ToString());
        if (request == null) return false;

        // 2. 生成 6 位随机验证码
        var isEmail = request.Account.Contains('@');
        var code = isEmail
            ? Guid.NewGuid().ToString("N")[..6].ToUpper()
            : new Random().Next(100000, 999999).ToString();

        // 3. 存储验证码到 Redis (与 Token 关联)，有效期 15 分钟
        await db.StringSetAsync(CacheKeys.RegistrationCode(regToken), code, TimeSpan.FromMinutes(15));

        // 4. 发送验证码

        if (isEmail)
        {
            var subject = "欢迎加入 MarketOurs - 验证您的注册信息";
            return await emailService.SendEmailWithTemplateAsync(request.Account, subject, UserService.VerificationEmailTemplate,
                new { token = code });
        }

        try
        {
            // 发送短信验证码
            var response = await smsService.RequestAsync("sms.message.send", new UniSmsModel()
            {
                To = request.Account,
                Signature = smsConfig.Signature,
                TemplateId = "pub_verif_ttl3",
                TemplateData = new Dictionary<string, object>()
                {
                    ["code"] = code,
                    ["ttl"] = 15
                }
            });

            return response is UniResponse { Code: "0" };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "发送注册短信验证码失败: {Account}", request.Account);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<UserDto> VerifyAndRegisterAsync(string regToken, string code)
    {
        if (_redis == null) throw new BusinessException(ErrorCode.CacheOperationFailed, "Redis 服务不可用");
        var db = _redis.GetDatabase();

        // 1. 验证验证码
        var cachedCode = await db.StringGetAsync(CacheKeys.RegistrationCode(regToken));
        if (!cachedCode.HasValue || cachedCode.ToString() != code)
        {
            throw new AuthException(ErrorCode.InvalidToken, "验证码无效或已过期");
        }

        // 2. 获取预注册数据
        var json = await db.StringGetAsync(CacheKeys.PreRegisterData(regToken));
        if (!json.HasValue)
        {
            throw new AuthException(ErrorCode.InvalidToken, "注册信息已过期");
        }

        var request = JsonSerializer.Deserialize<UserCreateDto>(json.ToString());
        if (request == null) throw new BusinessException(ErrorCode.DataProcessingFailed, "解析注册信息失败");

        // 3. 创建用户并激活
        var user = await userService.CreateAsync(request);

        // 4. 清理 Redis
        await db.KeyDeleteAsync(CacheKeys.PreRegisterData(regToken));
        await db.KeyDeleteAsync(CacheKeys.RegistrationCode(regToken));

        logger.LogInformation("用户 {Account} 验证通过，注册成功", request.Account);
        return user;
    }

    /// <inheritdoc/>
    public async Task<bool> SendLoginCodeAsync(string account)
    {
        if (_redis == null) throw new BusinessException(ErrorCode.CacheOperationFailed, "Redis 服务不可用");
        var db = _redis.GetDatabase();

        // 1. 生成 6 位随机验证码
        var isEmail = account.Contains('@');
        var code = isEmail
            ? Guid.NewGuid().ToString("N")[..6].ToUpper()
            : new Random().Next(100000, 999999).ToString();

        // 2. 存储验证码到 Redis，有效期 5 分钟
        await db.StringSetAsync(CacheKeys.LoginCode(account), code, TimeSpan.FromMinutes(5));

        // 3. 发送验证码
        if (isEmail)
        {
            var subject = "MarketOurs - 登录验证码";
            return await emailService.SendEmailWithTemplateAsync(account, subject, UserService.VerificationEmailTemplate,
                new { token = code });
        }

        try
        {
            var response = await smsService.RequestAsync("sms.message.send", new UniSmsModel()
            {
                To = account,
                Signature = smsConfig.Signature,
                TemplateId = "pub_verif_ttl3",
                TemplateData = new Dictionary<string, object>()
                {
                    ["code"] = code,
                    ["ttl"] = 5
                }
            });

            return response is UniResponse { Code: "0" };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "发送登录验证码失败: {Account}", account);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<TokenDto> LoginByCodeAsync(string account, string code, string deviceType)
    {
        if (_redis == null) throw new BusinessException(ErrorCode.CacheOperationFailed, "Redis 服务不可用");
        var db = _redis.GetDatabase();

        // 1. 验证验证码
        var cachedCode = await db.StringGetAsync(CacheKeys.LoginCode(account));
        if (!cachedCode.HasValue || cachedCode.ToString() != code)
        {
            throw new AuthException(ErrorCode.InvalidToken, "验证码无效或已过期");
        }

        // 2. 获取用户，如果不存在则注册
        var user = await userService.GetByAccountAsync(account);
        if (user == null)
        {
            logger.LogInformation("验证码登录：用户 {Account} 不存在，自动注册", account);
            user = await userService.CreateAsync(new UserCreateDto
            {
                Account = account,
                Password = Guid.NewGuid().ToString("N"), // 随机密码
                Name = account.Split('@')[0], // 默认用户名
                Role = "User"
            });
        }
        else if (!user.IsActive)
        {
            throw new AuthException(ErrorCode.UserNotActive, "您的账号尚未激活或已被禁用");
        }

        // 3. 清理验证码
        await db.KeyDeleteAsync(CacheKeys.LoginCode(account));

        // 4. 生成 Token
        return await GenerateTokenForUser(user, deviceType);
    }
}