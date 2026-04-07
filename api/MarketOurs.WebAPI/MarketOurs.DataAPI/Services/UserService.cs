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
    Task<UserDto?> UpdateAsync(string id, UserUpdateDto updateDto);

    /// <summary>
    /// 删除用户
    /// </summary>
    /// <param name="id">ID</param>
    Task DeleteAsync(string id);

    /// <summary>
    /// 发送邮箱验证码/令牌
    /// </summary>
    Task<bool> SendVerificationEmailAsync(string userId);

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

public class UserService(
    IUserRepo userRepo,
    IEmailService emailService,
    ISmsService smsService,
    SmsConfig smsConfig,
    IEnumerable<IConnectionMultiplexer> redisEnumerable,
    ILogger<UserService> logger) : IUserService
{
    private readonly IConnectionMultiplexer? _redis = redisEnumerable.FirstOrDefault();

    public const string VerificationEmailTemplate = @"
        <div style='font-family: sans-serif; padding: 20px; border: 1px solid #eee; border-radius: 5px;'>
            <h2 style='color: #333;'>欢迎加入 MarketOurs</h2>
            <p>感谢您的注册！请使用以下验证码完成邮箱验证：</p>
            <div style='background: #f4f4f4; padding: 15px; font-size: 24px; font-weight: bold; text-align: center; letter-spacing: 5px; color: #007bff;'>
                {{ token }}
            </div>
            <p style='color: #666; font-size: 14px; margin-top: 20px;'>
                该验证码 24 小时内有效。如果您没有注册过 MarketOurs，请忽略此邮件。
            </p>
        </div>";

    public const string PasswordResetTemplate = @"
        <div style='font-family: sans-serif; padding: 20px; border: 1px solid #eee; border-radius: 5px;'>
            <h2 style='color: #333;'>重置您的密码</h2>
            <p>您好 {{ name }}，我们收到了重置您 MarketOurs 账号密码的请求。</p>
            <p>请使用以下验证码进行重置：</p>
            <div style='background: #fff3cd; padding: 15px; font-size: 24px; font-weight: bold; text-align: center; letter-spacing: 5px; color: #856404;'>
                {{ token }}
            </div>
            <p style='color: #666; font-size: 14px; margin-top: 20px;'>
                该验证码 1 小时内有效。如果您没有申请过重置密码，请务必检查您的账号安全。
            </p>
        </div>";

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
    public async Task<UserDto?> LoginAsync(string account, string password)
    {
        var user = await userRepo.GetByAccountAsync(account);
        if (user == null || !DataTool.IsOk(password, user.Password)) return null;

        // 只有激活的用户可以登录
        if (!user.IsActive)
        {
            throw new AuthException(ErrorCode.UserNotActive, "您的账号尚未激活或已被禁用，请先完成验证");
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
    public async Task<bool> SendVerificationEmailAsync(string userId)
    {
        var user = await userRepo.GetByIdAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.Email)) return false;

        // 生成 6 位随机验证码
        var token = Guid.NewGuid().ToString("N")[..6].ToUpper();

        if (_redis != null)
        {
            var db = _redis.GetDatabase();
            await db.StringSetAsync(CacheKeys.VerificationToken(token), userId, TimeSpan.FromHours(24));
        }
        else
        {
            logger.LogWarning("Redis 服务未找到，验证码无法存储。用户: {UserId}", userId);
            return false;
        }

        var subject = "欢迎加入 MarketOurs - 邮箱验证";
        return await emailService.SendEmailWithTemplateAsync(user.Email, subject, VerificationEmailTemplate,
            new { token });
    }

    /// <inheritdoc/>
    public async Task<bool> VerifyEmailAsync(string token)
    {
        if (_redis == null) return false;

        var db = _redis.GetDatabase();
        var userId = await db.StringGetAsync(CacheKeys.VerificationToken(token));

        if (!userId.HasValue) return false;

        var user = await userRepo.GetByIdAsync(userId.ToString());
        if (user == null) return false;

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
        if (user == null || string.IsNullOrEmpty(user.Phone)) return false;

        // 生成 6 位纯数字随机验证码
        var token = new Random().Next(100000, 999999).ToString();

        if (_redis != null)
        {
            var db = _redis.GetDatabase();
            await db.StringSetAsync(CacheKeys.VerificationToken(token), userId, TimeSpan.FromMinutes(15));
        }
        else
        {
            logger.LogWarning("Redis 服务未找到，验证码无法存储。用户: {UserId}", userId);
            return false;
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
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "发送手机验证码异常: {Phone}", user.Phone);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> VerifyPhoneCodeAsync(string token)
    {
        if (_redis == null) return false;

        var db = _redis.GetDatabase();
        var userId = await db.StringGetAsync(CacheKeys.VerificationToken(token));

        if (!userId.HasValue) return false;

        var user = await userRepo.GetByIdAsync(userId.ToString());
        if (user == null) return false;

        user.IsPhoneVerified = true;
        user.IsActive = true;
        await userRepo.UpdateAsync(user);

        await db.KeyDeleteAsync(CacheKeys.VerificationToken(token));

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> ForgotPasswordAsync(string account)
    {
        var user = await userRepo.GetByAccountAsync(account);
        if (user == null) return false;

        var token = Guid.NewGuid().ToString("N")[..6].ToUpper();

        if (_redis != null)
        {
            var db = _redis.GetDatabase();
            await db.StringSetAsync(CacheKeys.ResetToken(token), user.Id, TimeSpan.FromHours(1));
        }
        else
        {
            logger.LogWarning("Redis 服务未找到，重置码无法存储。用户: {Account}", account);
            return false;
        }

        if (!string.IsNullOrEmpty(user.Email) && account.Contains('@'))
        {
            var subject = "MarketOurs - 重置密码";
            return await emailService.SendEmailWithTemplateAsync(user.Email, subject, PasswordResetTemplate,
                new { name = user.Name, token });
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
                }) is UniResponse { Code: "0" };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "发送重置密码验证码异常: {Phone}", user.Phone);
                return false;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        if (_redis == null) return false;

        var db = _redis.GetDatabase();
        var userId = await db.StringGetAsync(CacheKeys.ResetToken(token));

        if (!userId.HasValue) return false;

        var user = await userRepo.GetByIdAsync(userId.ToString());
        if (user == null) return false;

        user.Password = newPassword.StringToHash();
        await userRepo.UpdateAsync(user);

        await db.KeyDeleteAsync(CacheKeys.ResetToken(token));

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdatePushTokenAsync(string userId, string token)
    {
        var user = await userRepo.GetByIdAsync(userId);
        if (user == null) return false;

        user.PushToken = token;
        await userRepo.UpdateAsync(user);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> ChangePasswordAsync(string userId, string oldPassword, string newPassword)
    {
        var user = await userRepo.GetByIdAsync(userId);
        if (user == null || !DataTool.IsOk(oldPassword, user.Password)) return false;

        user.Password = newPassword.StringToHash();
        await userRepo.UpdateAsync(user);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> SendVerificationCodeAsync(string account, string code)
    {
        var isEmail = account.Contains('@');

        if (isEmail)
        {
            var subject = "欢迎加入 MarketOurs - 验证您的注册信息";
            return await emailService.SendEmailWithTemplateAsync(account, subject, VerificationEmailTemplate,
                new { token = code });
        }

        try
        {
            // 发送短信验证码
            var response = await smsService.RequestAsync("sms.message.send", new UniSmsModel()
            {
                To = account,
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
            logger.LogError(ex, "发送通用验证码异常: {Account}", account);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<UserDto?> UpdateAsync(string id, UserUpdateDto updateDto)
    {
        var user = await userRepo.GetByIdAsync(id);
        if (user == null) return null;

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
            PushSettings = user.PushSettings
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
}
