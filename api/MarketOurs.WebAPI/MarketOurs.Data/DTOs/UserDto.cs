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
