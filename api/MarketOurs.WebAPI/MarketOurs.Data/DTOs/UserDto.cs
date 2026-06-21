using System.ComponentModel.DataAnnotations;

namespace MarketOurs.Data.DTOs;

/// <summary>
/// 用户数据传输对象
/// </summary>
public class UserDto
{
    /// <summary>
    /// 用户唯一标识
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 电子邮箱
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 手机号码
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// 用户名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 角色 (如 Admin, User)
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// 头像地址
    /// </summary>
    public string Avatar { get; set; } = string.Empty;

    /// <summary>
    /// 个人简介
    /// </summary>
    public string Info { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTime LastLoginAt { get; set; }

    /// <summary>
    /// 账户是否启用
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 邮箱是否已验证
    /// </summary>
    public bool IsEmailVerified { get; set; }

    /// <summary>
    /// 手机号是否已验证
    /// </summary>
    public bool IsPhoneVerified { get; set; }

    /// <summary>
    /// 推送设置 (JSON 格式)
    /// </summary>
    public string? PushSettings { get; set; }

    /// <summary>
    /// 当前已注册的推送 Provider
    /// </summary>
    public string? PushProvider { get; set; }

    /// <summary>
    /// 第三方绑定 ID
    /// </summary>
    public string? GithubId { get; set; }
    public string? GoogleId { get; set; }
    public string? WeixinId { get; set; }
    public string? OursId { get; set; }
}

/// <summary>
/// 公开用户主页数据传输对象
/// </summary>
public class PublicUserProfileDto
{
    /// <summary>
    /// 用户唯一标识
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 用户名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 角色
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// 头像地址
    /// </summary>
    public string Avatar { get; set; } = string.Empty;

    /// <summary>
    /// 个人简介
    /// </summary>
    public string Info { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 粉丝数量
    /// </summary>
    public int FollowerCount { get; set; }

    /// <summary>
    /// 关注数量
    /// </summary>
    public int FollowingCount { get; set; }

    /// <summary>
    /// 与当前查看者的关系状态（仅登录用户可见）
    /// </summary>
    public FollowStatsDto? RelationshipStatus { get; set; }
}

/// <summary>
/// 创建用户请求对象
/// </summary>
public class UserCreateDto
{
    /// <summary>
    /// 账号 (邮箱或手机号)
    /// </summary>
    [Required(ErrorMessage = "账号不能为空")] 
    [MaxLength(128, ErrorMessage = "账号长度不能超过128位")]
    public string Account { get; set; } = string.Empty;
    
    /// <summary>
    /// 密码
    /// </summary>
    [Required(ErrorMessage = "密码不能为空")] 
    [MinLength(6, ErrorMessage = "密码长度不能少于6位")]
    [MaxLength(128, ErrorMessage = "密码长度不能超过128位")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 用户名
    /// </summary>
    [Required(ErrorMessage = "用户名不能为空")]
    [MaxLength(128, ErrorMessage = "用户名长度不能超过128位")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 头像地址
    /// </summary>
    [MaxLength(128, ErrorMessage = "头像地址长度不能超过128位")]
    public string Avatar { get; set; } = string.Empty;

    /// <summary>
    /// 角色
    /// </summary>
    [MaxLength(128, ErrorMessage = "角色长度不能超过128位")]
    public string Role { get; set; } = "User";
}

/// <summary>
/// 更新用户请求对象
/// </summary>
public class UserUpdateDto
{
    /// <summary>
    /// 用户名
    /// </summary>
    [MaxLength(128, ErrorMessage = "用户名长度不能超过128位")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 电子邮箱
    /// </summary>
    [MaxLength(128, ErrorMessage = "邮箱长度不能超过128位")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string? Email { get; set; }

    /// <summary>
    /// 手机号码
    /// </summary>
    [MaxLength(32, ErrorMessage = "手机号长度不能超过32位")]
    [Phone(ErrorMessage = "手机号格式不正确")]
    public string? Phone { get; set; }

    /// <summary>
    /// 头像地址
    /// </summary>
    [MaxLength(128, ErrorMessage = "头像地址长度不能超过128位")]
    public string Avatar { get; set; } = string.Empty;

    /// <summary>
    /// 个人简介
    /// </summary>
    [MaxLength(1024, ErrorMessage = "个人简介长度不能超过1024位")]
    public string Info { get; set; } = string.Empty;

    /// <summary>
    /// 第三方绑定 ID (仅允许绑定操作，不可随便修改)
    /// </summary>
    public string? GithubId { get; set; }
    public string? GoogleId { get; set; }
    public string? WeixinId { get; set; }
    public string? OursId { get; set; }
}

/// <summary>
/// 管理员重置用户密码的请求对象
/// </summary>
public class AdminResetPasswordRequest
{
    /// <summary>
    /// 新密码 (不需要旧密码)
    /// </summary>
    [Required(ErrorMessage = "新密码不能为空")]
    [MinLength(6, ErrorMessage = "新密码长度不能少于6位")]
    [MaxLength(128, ErrorMessage = "新密码长度不能超过128位")]
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// 更新用户推送 token 的请求对象
/// </summary>
public class UpdatePushTokenRequest
{
    /// <summary>
    /// 推送 Provider 标识，如 jpush
    /// </summary>
    [MaxLength(32, ErrorMessage = "推送 Provider 长度不能超过32位")]
    public string? Provider { get; set; }

    /// <summary>
    /// 设备推送 token / registrationId
    /// </summary>
    [MaxLength(2048, ErrorMessage = "推送 Token 长度不能超过2048位")]
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// 修改密码请求对象
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// 旧密码
    /// </summary>
    [Required(ErrorMessage = "旧密码不能为空")]
    public string OldPassword { get; set; } = string.Empty;

    /// <summary>
    /// 新密码
    /// </summary>
    [Required(ErrorMessage = "新密码不能为空")]
    [MinLength(6, ErrorMessage = "新密码长度不能少于6位")]
    [MaxLength(128, ErrorMessage = "新密码长度不能超过128位")]
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// 用户简单信息数据传输对象
/// </summary>
public class UserSimpleDto
{
    /// <summary>
    /// 用户唯一标识
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 用户名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 头像地址
    /// </summary>
    public string Avatar { get; set; } = string.Empty;
}
