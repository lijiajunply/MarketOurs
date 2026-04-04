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
    Task<UserDto?> GetByEmailAsync(string email);
    Task<UserDto?> LoginAsync(string email, string password);
    Task<UserDto> CreateAsync(UserCreateDto createDto);
    Task<UserDto?> UpdateAsync(string id, UserUpdateDto updateDto);
    Task DeleteAsync(string id);

    // Email verification & Password reset
    Task<bool> SendVerificationEmailAsync(string userId);
    Task<bool> VerifyEmailAsync(string token);
    Task<bool> ForgotPasswordAsync(string email);
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

    public async Task<UserDto?> GetByEmailAsync(string email)
    {
        var user = await userRepo.GetByEmailAsync(email);
        return user != null ? MapToDto(user) : null;
    }

    public async Task<UserDto?> LoginAsync(string email, string password)
    {
        var user = await userRepo.GetByEmailAsync(email);
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
        var user = new UserModel
        {
            Email = createDto.Email,
            Password = createDto.Password.StringToHash(),
            Name = createDto.Name,
            Role = createDto.Role,
            CreatedAt = DateTime.Now,
            LastLoginAt = DateTime.Now,
            IsActive = true, // 初始化默认激活，不需要强制邮箱验证
            IsEmailVerified = false
        };
        await userRepo.CreateAsync(user);
        
        return MapToDto(user);
    }

    public async Task<bool> SendVerificationEmailAsync(string userId)
    {
        var user = await userRepo.GetByIdAsync(userId);
        if (user == null) return false;

        // 生成 6 位随机验证码
        var token = Guid.NewGuid().ToString("N")[..6].ToUpper();
        
        if (_redis != null)
        {
            var db = _redis.GetDatabase();
            // 存入 Redis，Key 为 Token，Value 为 UserId，过期时间 24 小时
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

        // 验证成功后删除 Redis 中的验证码
        await db.KeyDeleteAsync(CacheKeys.VerificationToken(token));

        return true;
    }

    public async Task<bool> ForgotPasswordAsync(string email)
    {
        var user = await userRepo.GetByEmailAsync(email);
        if (user == null) return false;

        var token = Guid.NewGuid().ToString("N")[..6].ToUpper();
        
        if (_redis != null)
        {
            var db = _redis.GetDatabase();
            // 存入 Redis，Key 为 Token，Value 为 UserId，过期时间 1 小时
            await db.StringSetAsync(CacheKeys.ResetToken(token), user.Id, TimeSpan.FromHours(1));
        }
        else
        {
            logger.LogWarning("Redis 服务未找到，重置码无法存储。用户: {Email}", email);
            return false;
        }

        var subject = "MarketOurs - 重置密码";
        var body = $"您正在申请重置密码，验证码是: <b>{token}</b><br/>该验证码 1 小时内有效。";
        return await emailService.SendEmailAsync(user.Email, subject, body, true);
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

        // 重置成功后删除 Redis 中的重置码
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
            Name = user.Name,
            Role = user.Role,
            Avatar = user.Avatar,
            Info = user.Info,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            IsActive = user.IsActive
        };
    }
}
