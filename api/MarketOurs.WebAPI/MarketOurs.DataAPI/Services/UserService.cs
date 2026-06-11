using System.Text.Json;
using MarketOurs.Data;
using MarketOurs.Data.DataModels;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Configs;
using MarketOurs.DataAPI.Exceptions;
using MarketOurs.DataAPI.Repos;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MarketOurs.DataAPI.Services;

/// <summary>
/// 用户服务接口，提供用户管理、认证、验证（邮箱/手机）及密码重置等功能
/// </summary>
public interface IUserService
{
    /// <summary>
    /// 获取所有用户 (分页)
    /// </summary>
    /// <param name="params">分页查询参数</param>
    Task<PagedResultDto<UserDto>> GetAllAsync(PaginationParams @params);

    /// <summary>
    /// 搜索用户 (基于关键词)
    /// </summary>
    /// <param name="params">分页查询参数</param>
    Task<PagedResultDto<UserDto>> SearchAsync(PaginationParams @params);

    /// <summary>
    /// 根据 ID 获取用户详情
    /// </summary>
    /// <param name="id">id</param>
    Task<UserDto?> GetByIdAsync(string id);

    /// <summary>
    /// 根据 ID 获取公开用户资料
    /// </summary>
    /// <param name="id">id</param>
    Task<PublicUserProfileDto?> GetPublicProfileByIdAsync(string id);

    /// <summary>
    /// 根据 邮箱或手机号 获取用户详情
    /// </summary>
    /// <param name="account">邮箱或手机号</param>
    Task<UserDto?> GetByAccountAsync(string account);

    /// <summary>
    /// 根据 第三方平台 ID 获取用户详情
    /// </summary>
    Task<UserDto?> GetByThirdPartyIdAsync(string provider, string providerId);

    /// <summary>
    /// 用户登录验证
    /// </summary>
    /// <param name="account">账号</param>
    /// <param name="password">密码</param>
    /// <returns>用户 DTO，验证失败返回 null</returns>
    Task<UserDto?> LoginAsync(string account, string password);

    /// <summary>
    /// 创建新用户（管理员注册或第三方平台注册）
    /// </summary>
    Task<UserDto> CreateAsync(UserCreateDto createDto);

    /// <summary>
    /// 更新用户信息
    /// </summary>
    /// <param name="id">ID</param>
    /// <param name="updateDto">更新数据 DTO</param>
    Task<UserDto> UpdateAsync(string id, UserUpdateDto updateDto);

    /// <summary>
    /// 删除用户
    /// </summary>
    /// <param name="id">ID</param>
    Task DeleteAsync(string id);

    /// <summary>
    /// 发送邮箱验证码/令牌
    /// </summary>
    Task<bool> SendVerificationEmailAsync(string userId,
        EmailVerificationPurpose purpose = EmailVerificationPurpose.EmailVerification);

    /// <summary>
    /// 验证邮箱令牌
    /// </summary>
    Task<bool> VerifyEmailAsync(string token);

    /// <summary>
    /// 发送手机验证码
    /// </summary>
    Task<bool> SendPhoneVerificationCodeAsync(string userId);

    /// <summary>
    /// 验证手机验证码
    /// </summary>
    Task<bool> VerifyPhoneCodeAsync(string token);

    /// <summary>
    /// 校验当前登录用户的本次邮箱/手机验证码。
    /// </summary>
    Task<bool> VerifyCurrentUserCodeAsync(string userId, string token, string channel);

    /// <summary>
    /// 清除用户的第三方平台绑定。
    /// </summary>
    Task<UserDto> ClearThirdPartyBindingAsync(string userId, string provider);

    /// <summary>
    /// 忘记密码：发送重置令牌
    /// </summary>
    Task<bool> ForgotPasswordAsync(string account);

    /// <summary>
    /// 重置密码
    /// </summary>
    Task<bool> ResetPasswordAsync(string token, string newPassword);

    /// <summary>
    /// 更新用户的推送 Token (用于移动端推送)
    /// </summary>
    Task<bool> UpdatePushTokenAsync(string userId, string token);

    /// <summary>
    /// 修改密码
    /// </summary>
    Task<bool> ChangePasswordAsync(string userId, string oldPassword, string newPassword);
}

public enum EmailVerificationPurpose
{
    EmailVerification,
    ThirdPartyUnbind
}

public class UserService(
    IUserRepo userRepo,
    IEmailService emailService,
    ISmsService smsService,
    SmsConfig smsConfig,
    IEnumerable<IConnectionMultiplexer> redisEnumerable,
    ILogger<UserService> logger) : IUserService
{
    private readonly IConnectionMultiplexer? _redis = redisEnumerable.FirstOrDefault();

    private sealed record VerificationTokenPayload(string UserId, string Channel);

    /// <inheritdoc/>
    public async Task<PagedResultDto<UserDto>> GetAllAsync(PaginationParams @params)
    {
        var totalCount = await userRepo.CountAsync();
        var users = await userRepo.GetAllAsync(@params.PageIndex, @params.PageSize);
        return PagedResultDto<UserDto>.Success(users.Select(MapToDto).ToList(), totalCount, @params.PageIndex,
            @params.PageSize);
    }

    /// <inheritdoc/>
    public async Task<PagedResultDto<UserDto>> SearchAsync(PaginationParams @params)
    {
        if (string.IsNullOrWhiteSpace(@params.Keyword))
            return PagedResultDto<UserDto>.Success([], 0, @params.PageIndex, @params.PageSize);

        var totalCount = await userRepo.SearchCountAsync(@params.Keyword);
        var users = await userRepo.SearchAsync(@params.Keyword, @params.PageIndex, @params.PageSize);
        return PagedResultDto<UserDto>.Success(users.Select(MapToDto).ToList(), totalCount, @params.PageIndex,
            @params.PageSize);
    }

    /// <inheritdoc/>
    public async Task<UserDto?> GetByIdAsync(string id)
    {
        var user = await userRepo.GetByIdAsync(id);
        return user != null ? MapToDto(user) : null;
    }

    /// <inheritdoc/>
    public async Task<PublicUserProfileDto?> GetPublicProfileByIdAsync(string id)
    {
        var user = await userRepo.GetByIdAsync(id);
        return user != null ? MapToPublicProfileDto(user) : null;
    }

    /// <inheritdoc/>
    public async Task<UserDto?> GetByAccountAsync(string account)
    {
        var user = await userRepo.GetByAccountAsync(account);
        return user != null ? MapToDto(user) : null;
    }

    /// <inheritdoc/>
    public async Task<UserDto?> GetByThirdPartyIdAsync(string provider, string providerId)
    {
        var user = await userRepo.GetByThirdPartyIdAsync(provider, providerId);
        return user != null ? MapToDto(user) : null;
    }

    /// <inheritdoc/>
    public async Task<UserDto?> LoginAsync(string account, string password)
    {
        var user = await userRepo.GetByAccountAsync(account);
        if (user == null || !DataTool.IsOk(password, user.Password)) return null;

        // 只有激活的用户可以登录
        if (!user.IsActive)
        {
            throw new AuthException(ErrorCode.AccountNotActive, "您的账号尚未激活或已被禁用，请先完成验证", 403);
        }

        return MapToDto(user);
    }

    /// <inheritdoc/>
    public async Task<UserDto> CreateAsync(UserCreateDto createDto)
    {
        var isEmail = createDto.Account.Contains('@');

        var user = new UserModel
        {
            Email = isEmail ? createDto.Account : string.Empty,
            Phone = !isEmail ? createDto.Account : string.Empty,
            Password = createDto.Password.StringToHash(),
            Name = createDto.Name,
            Avatar = createDto.Avatar,
            Role = createDto.Role,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            IsActive = true, // 创建时激活，不需要强制验证
            IsEmailVerified = false,
            IsPhoneVerified = false
        };
        await userRepo.CreateAsync(user);

        return MapToDto(user);
    }

    /// <inheritdoc/>
    public async Task<bool> SendVerificationEmailAsync(string userId,
        EmailVerificationPurpose purpose = EmailVerificationPurpose.EmailVerification)
    {
        var user = await userRepo.GetByIdAsync(userId);
        if (user == null) throw new ResourceAccessException(ErrorCode.UserNotFound, "用户不存在");
        if (string.IsNullOrEmpty(user.Email)) throw new BusinessException(ErrorCode.ParameterEmpty, "用户未绑定邮箱");

        // 生成 6 位随机验证码
        var token = Guid.NewGuid().ToString("N")[..6].ToUpper();
        var (subject, template, ttl) = purpose switch
        {
            EmailVerificationPurpose.ThirdPartyUnbind => (
                "MarketOurs - 解绑第三方账号验证码",
                EmailTemplates.ThirdPartyUnbindCode,
                TimeSpan.FromMinutes(15)),
            _ => (
                "欢迎加入 MarketOurs - 邮箱验证",
                EmailTemplates.EmailVerification,
                TimeSpan.FromHours(24))
        };

        if (_redis != null)
        {
            var db = _redis.GetDatabase();
            await db.StringSetAsync(CacheKeys.VerificationToken(token), BuildVerificationPayload(userId, "email"),
                ttl);
        }
        else
        {
            logger.LogWarning("Redis 服务未找到，验证码无法存储。用户: {UserId}", userId);
            throw new BusinessException(ErrorCode.CacheOperationFailed, "Redis 服务不可用");
        }

        var sent = await emailService.SendEmailWithTemplateAsync(user.Email, subject, template,
            new { token });
        if (!sent) throw new BusinessException(ErrorCode.ExternalServiceFailed, "邮箱验证码发送失败");
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> VerifyEmailAsync(string token)
    {
        if (_redis == null) throw new BusinessException(ErrorCode.CacheOperationFailed, "Redis 服务不可用");

        var db = _redis.GetDatabase();
        var payload = await ReadVerificationPayloadAsync(db, token);

        if (payload == null ||
            (!string.Equals(payload.Channel, "email", StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(payload.Channel, "legacy", StringComparison.OrdinalIgnoreCase)))
            throw new AuthException(ErrorCode.InvalidToken, "验证码无效或已过期");

        var user = await userRepo.GetByIdAsync(payload.UserId);
        if (user == null) throw new ResourceAccessException(ErrorCode.UserNotFound, "用户不存在");

        user.IsEmailVerified = true;
        user.IsActive = true;
        await userRepo.UpdateAsync(user);

        await db.KeyDeleteAsync(CacheKeys.VerificationToken(token));

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> SendPhoneVerificationCodeAsync(string userId)
    {
        var user = await userRepo.GetByIdAsync(userId);
        if (user == null) throw new ResourceAccessException(ErrorCode.UserNotFound, "用户不存在");
        if (string.IsNullOrEmpty(user.Phone)) throw new BusinessException(ErrorCode.ParameterEmpty, "用户未绑定手机号");

        // 生成 6 位纯数字随机验证码
        var token = new Random().Next(100000, 999999).ToString();

        if (_redis != null)
        {
            var db = _redis.GetDatabase();
            await db.StringSetAsync(CacheKeys.VerificationToken(token), BuildVerificationPayload(userId, "phone"),
                TimeSpan.FromMinutes(15));
        }
        else
        {
            logger.LogWarning("Redis 服务未找到，验证码无法存储。用户: {UserId}", userId);
            throw new BusinessException(ErrorCode.CacheOperationFailed, "Redis 服务不可用");
        }

        try
        {
            // 发送短信验证码
            var response = await smsService.RequestAsync("sms.message.send", new UniSmsModel()
            {
                To = user.Phone,
                Signature = smsConfig.Signature,
                TemplateId = "pub_verif_ttl3",
                TemplateData = new Dictionary<string, object>()
                {
                    ["code"] = token,
                    ["ttl"] = 15
                }
            });

            if (response is UniResponse { Code: "0" })
            {
                logger.LogInformation("Successfully sent verification code to phone {Phone}", user.Phone);
                return true;
            }

            logger.LogWarning("Failed to send verification code to phone {Phone}", user.Phone);
            throw new BusinessException(ErrorCode.ExternalServiceFailed, "短信验证码发送失败");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "发送手机验证码异常: {Phone}", user.Phone);
            throw new BusinessException(ErrorCode.ExternalServiceFailed, "短信验证码发送失败", innerException: ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> VerifyPhoneCodeAsync(string token)
    {
        if (_redis == null) throw new BusinessException(ErrorCode.CacheOperationFailed, "Redis 服务不可用");

        var db = _redis.GetDatabase();
        var payload = await ReadVerificationPayloadAsync(db, token);

        if (payload == null ||
            (!string.Equals(payload.Channel, "phone", StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(payload.Channel, "legacy", StringComparison.OrdinalIgnoreCase)))
            throw new AuthException(ErrorCode.InvalidToken, "验证码无效或已过期");

        var user = await userRepo.GetByIdAsync(payload.UserId);
        if (user == null) throw new ResourceAccessException(ErrorCode.UserNotFound, "用户不存在");

        user.IsPhoneVerified = true;
        user.IsActive = true;
        await userRepo.UpdateAsync(user);

        await db.KeyDeleteAsync(CacheKeys.VerificationToken(token));

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> VerifyCurrentUserCodeAsync(string userId, string token, string channel)
    {
        if (_redis == null) throw new BusinessException(ErrorCode.CacheOperationFailed, "Redis 服务不可用");

        var normalizedChannel = NormalizeVerificationChannel(channel);
        var db = _redis.GetDatabase();
        var payload = await ReadVerificationPayloadAsync(db, token);

        if (payload == null ||
            !string.Equals(payload.UserId, userId, StringComparison.Ordinal) ||
            !string.Equals(payload.Channel, normalizedChannel, StringComparison.OrdinalIgnoreCase))
        {
            throw new AuthException(ErrorCode.InvalidToken, "验证码无效或已过期");
        }

        var user = await userRepo.GetByIdAsync(userId);
        if (user == null) throw new ResourceAccessException(ErrorCode.UserNotFound, "用户不存在");

        if (normalizedChannel == "email")
        {
            user.IsEmailVerified = true;
        }
        else
        {
            user.IsPhoneVerified = true;
        }

        user.IsActive = true;
        await userRepo.UpdateAsync(user);
        await db.KeyDeleteAsync(CacheKeys.VerificationToken(token));

        return true;
    }

    /// <inheritdoc/>
    public async Task<UserDto> ClearThirdPartyBindingAsync(string userId, string provider)
    {
        var user = await userRepo.GetByIdAsync(userId);
        if (user == null) throw new ResourceAccessException(ErrorCode.UserNotFound, "用户不存在");

        switch (provider?.ToLowerInvariant())
        {
            case "github":
                EnsureBound(user.GithubId);
                user.GithubId = null;
                break;
            case "google":
                EnsureBound(user.GoogleId);
                user.GoogleId = null;
                break;
            case "weixin":
                EnsureBound(user.WeixinId);
                user.WeixinId = null;
                break;
            case "ours":
                EnsureBound(user.OursId);
                user.OursId = null;
                break;
            default:
                throw new BusinessException(ErrorCode.ParameterFormatError, "不支持的第三方平台");
        }

        await userRepo.UpdateAsync(user);
        return MapToDto(user);

        static void EnsureBound(string? providerId)
        {
            if (string.IsNullOrWhiteSpace(providerId))
            {
                throw new BusinessException(ErrorCode.InvalidStatusForOperation, "该第三方账号尚未绑定");
            }
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ForgotPasswordAsync(string account)
    {
        var user = await userRepo.GetByAccountAsync(account);
        if (user == null) throw new ResourceAccessException(ErrorCode.UserNotFound, "该账号未注册");

        var token = Guid.NewGuid().ToString("N")[..6].ToUpper();

        if (_redis != null)
        {
            var db = _redis.GetDatabase();
            await db.StringSetAsync(CacheKeys.ResetToken(token), user.Id, TimeSpan.FromHours(1));
        }
        else
        {
            logger.LogWarning("Redis 服务未找到，重置码无法存储。用户: {Account}", account);
            throw new BusinessException(ErrorCode.CacheOperationFailed, "Redis 服务不可用");
        }

        if (!string.IsNullOrEmpty(user.Email) && account.Contains('@'))
        {
            var subject = "MarketOurs - 重置密码";
            var sent = await emailService.SendEmailWithTemplateAsync(user.Email, subject, EmailTemplates.PasswordReset,
                new { name = user.Name, token });
            if (!sent) throw new BusinessException(ErrorCode.ExternalServiceFailed, "重置密码邮件发送失败");
            return true;
        }

        if (!string.IsNullOrEmpty(user.Phone))
        {
            try
            {
                // 发送短信重置码
                return await smsService.RequestAsync("sms.message.send", new UniSmsModel()
                {
                    To = user.Phone,
                    Signature = smsConfig.Signature,
                    TemplateId = "pub_verif_ttl3",
                    TemplateData = new Dictionary<string, object>()
                    {
                        ["code"] = token,
                        ["ttl"] = 15
                    }
                }) is UniResponse { Code: "0" }
                    ? true
                    : throw new BusinessException(ErrorCode.ExternalServiceFailed, "重置密码短信发送失败");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "发送重置密码验证码异常: {Phone}", user.Phone);
                throw new BusinessException(ErrorCode.ExternalServiceFailed, "重置密码短信发送失败", innerException: ex);
            }
        }

        throw new BusinessException(ErrorCode.OperationFailed, "该账号无法接收重置验证码");
    }

    /// <inheritdoc/>
    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        if (_redis == null) throw new BusinessException(ErrorCode.CacheOperationFailed, "Redis 服务不可用");

        var db = _redis.GetDatabase();
        var userId = await db.StringGetAsync(CacheKeys.ResetToken(token));

        if (!userId.HasValue) throw new AuthException(ErrorCode.InvalidToken, "验证码无效或已过期");

        var user = await userRepo.GetByIdAsync(userId.ToString());
        if (user == null) throw new ResourceAccessException(ErrorCode.UserNotFound, "用户不存在");

        user.Password = newPassword.StringToHash();
        await userRepo.UpdateAsync(user);

        await db.KeyDeleteAsync(CacheKeys.ResetToken(token));

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdatePushTokenAsync(string userId, string token)
    {
        var user = await userRepo.GetByIdAsync(userId);
        if (user == null) throw new ResourceAccessException(ErrorCode.UserNotFound, "用户不存在");

        user.PushToken = token;
        await userRepo.UpdateAsync(user);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> ChangePasswordAsync(string userId, string oldPassword, string newPassword)
    {
        var user = await userRepo.GetByIdAsync(userId);
        if (user == null) throw new ResourceAccessException(ErrorCode.UserNotFound, "用户不存在");
        if (!DataTool.IsOk(oldPassword, user.Password))
            throw new ValidationException(ErrorCode.PasswordMismatch, "旧密码错误");

        user.Password = newPassword.StringToHash();
        await userRepo.UpdateAsync(user);
        return true;
    }

    /// <inheritdoc/>
    public async Task<UserDto> UpdateAsync(string id, UserUpdateDto updateDto)
    {
        var user = await userRepo.GetByIdAsync(id);
        if (user == null) throw new ResourceAccessException(ErrorCode.UserNotFound, "用户不存在");

        if (!string.IsNullOrEmpty(updateDto.Name))
            user.Name = updateDto.Name;

        if (!string.IsNullOrEmpty(updateDto.Avatar))
            user.Avatar = updateDto.Avatar;

        user.Info = updateDto.Info;

        // 如果修改了邮箱或手机号，标记为未验证
        if (updateDto.Email != null && updateDto.Email != user.Email)
        {
            user.Email = updateDto.Email;
            user.IsEmailVerified = false;
        }

        if (updateDto.Phone != null && updateDto.Phone != user.Phone)
        {
            user.Phone = updateDto.Phone;
            user.IsPhoneVerified = false;
        }

        if (updateDto.GithubId != null) user.GithubId = updateDto.GithubId;
        if (updateDto.GoogleId != null) user.GoogleId = updateDto.GoogleId;
        if (updateDto.WeixinId != null) user.WeixinId = updateDto.WeixinId;
        if (updateDto.OursId != null) user.OursId = updateDto.OursId;

        await userRepo.UpdateAsync(user);
        return MapToDto(user);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string id)
    {
        await userRepo.DeleteAsync(id);
    }

    public static UserDto MapToDto(UserModel user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Phone = user.Phone,
            Name = user.Name,
            Role = user.Role,
            Avatar = user.Avatar,
            Info = user.Info,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            IsActive = user.IsActive,
            IsEmailVerified = user.IsEmailVerified,
            IsPhoneVerified = user.IsPhoneVerified,
            PushSettings = user.PushSettings,
            GithubId = user.GithubId,
            GoogleId = user.GoogleId,
            WeixinId = user.WeixinId,
            OursId = user.OursId
        };
    }

    public static PublicUserProfileDto MapToPublicProfileDto(UserModel user)
    {
        return new PublicUserProfileDto
        {
            Id = user.Id,
            Name = user.Name,
            Role = user.Role,
            Avatar = user.Avatar,
            Info = user.Info,
            CreatedAt = user.CreatedAt
        };
    }

    private static string BuildVerificationPayload(string userId, string channel)
    {
        return JsonSerializer.Serialize(new VerificationTokenPayload(userId, channel));
    }

    private static async Task<VerificationTokenPayload?> ReadVerificationPayloadAsync(IDatabase db, string token)
    {
        var value = await db.StringGetAsync(CacheKeys.VerificationToken(token));
        if (!value.HasValue) return null;

        var raw = value.ToString();
        if (string.IsNullOrWhiteSpace(raw)) return null;

        if (!raw.TrimStart().StartsWith('{'))
        {
            return new VerificationTokenPayload(raw, "legacy");
        }

        try
        {
            return JsonSerializer.Deserialize<VerificationTokenPayload>(raw);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string NormalizeVerificationChannel(string channel)
    {
        return channel?.ToLowerInvariant() switch
        {
            "email" => "email",
            "phone" => "phone",
            _ => throw new BusinessException(ErrorCode.ParameterFormatError, "不支持的验证方式")
        };
    }
}
