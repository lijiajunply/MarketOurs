using MarketOurs.Data;
using MarketOurs.Data.DataModels;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Configs;
using MarketOurs.DataAPI.Exceptions;
using MarketOurs.DataAPI.Repos;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MarketOurs.DataAPI.Services;

public interface IUserService
{
    Task<List<UserDto>> GetAllAsync();
    Task<UserDto?> GetByIdAsync(string id);
    Task<UserDto?> GetByAccountAsync(string account);
    Task<UserDto?> LoginAsync(string account, string password);
    Task<UserDto> CreateAsync(UserCreateDto createDto);
    Task<UserDto?> UpdateAsync(string id, UserUpdateDto updateDto);
    Task DeleteAsync(string id);

    // Email verification & Password reset
    Task<bool> SendVerificationEmailAsync(string userId);
    Task<bool> VerifyEmailAsync(string token);

    // Phone verification
    Task<bool> SendPhoneVerificationCodeAsync(string userId);
    Task<bool> VerifyPhoneCodeAsync(string token);

    Task<bool> ForgotPasswordAsync(string account);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
}

public class UserService(
    IUserRepo userRepo,
    IEmailService emailService,
    IEnumerable<IConnectionMultiplexer> redisEnumerable,
    ILogger<UserService> logger) : IUserService
{
    private readonly IConnectionMultiplexer? _redis = redisEnumerable.FirstOrDefault();

    public async Task<List<UserDto>> GetAllAsync()
    {
        var users = await userRepo.GetAllAsync();
        return users.Select(MapToDto).ToList();
    }

    public async Task<UserDto?> GetByIdAsync(string id)
    {
        var user = await userRepo.GetByIdAsync(id);
        return user != null ? MapToDto(user) : null;
    }

    public async Task<UserDto?> GetByAccountAsync(string account)
    {
        var user = await userRepo.GetByAccountAsync(account);
        return user != null ? MapToDto(user) : null;
    }

    public async Task<UserDto?> LoginAsync(string account, string password)
    {
        var user = await userRepo.GetByAccountAsync(account);
        if (user == null || !DataTool.IsOk(password, user.Password)) return null;

        // 只有激活的用户可以登录
        if (!user.IsActive)
        {
            throw new AuthException(ErrorCode.UserLocked, "您的账号已被锁定或禁用");
        }

        return MapToDto(user);
    }

    public async Task<UserDto> CreateAsync(UserCreateDto createDto)
    {
        var isEmail = createDto.Account.Contains('@');

        var user = new UserModel
        {
            Email = isEmail ? createDto.Account : string.Empty,
            Phone = !isEmail ? createDto.Account : string.Empty,
            Password = createDto.Password.StringToHash(),
            Name = createDto.Name,
            Role = createDto.Role,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            IsActive = true, // 初始化默认激活，不需要强制验证
            IsEmailVerified = false,
            IsPhoneVerified = false
        };
        await userRepo.CreateAsync(user);

        return MapToDto(user);
    }

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
        var body = $"您的验证码是: <b>{token}</b><br/>该验证码 24 小时内有效。";
        return await emailService.SendEmailAsync(user.Email, subject, body, true);
    }

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

        // 模拟短信发送
        logger.LogInformation($"[Mock SMS] Sending verification code {token} to phone {user.Phone}");
        return true;
    }

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
            var body = $"您正在申请重置密码，验证码是: <b>{token}</b><br/>该验证码 1 小时内有效。";
            return await emailService.SendEmailAsync(user.Email, subject, body, true);
        }

        if (!string.IsNullOrEmpty(user.Phone))
        {
            // 模拟短信发送
            logger.LogInformation($"[Mock SMS] Sending reset code {token} to phone {user.Phone}");
            return true;
        }

        return false;
    }

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

    public async Task<UserDto?> UpdateAsync(string id, UserUpdateDto updateDto)
    {
        var user = await userRepo.GetByIdAsync(id);
        if (user == null) return null;

        user.Name = updateDto.Name;
        user.Avatar = updateDto.Avatar;
        user.Info = updateDto.Info;

        await userRepo.UpdateAsync(user);
        return MapToDto(user);
    }

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
            IsPhoneVerified = user.IsPhoneVerified
        };
    }
}