using System.ComponentModel.DataAnnotations;

namespace MarketOurs.Data.DTOs;

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public string Info { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool IsPhoneVerified { get; set; }
}

public class UserCreateDto
{
    /// <summary>
    /// Email or Phone number
    /// </summary>
    [Required(ErrorMessage = "账号不能为空")] 
    [MaxLength(128, ErrorMessage = "账号长度不能超过128位")]
    public string Account { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "密码不能为空")] 
    [MinLength(6, ErrorMessage = "密码长度不能少于6位")]
    [MaxLength(128, ErrorMessage = "密码长度不能超过128位")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "用户名不能为空")]
    [MaxLength(128, ErrorMessage = "用户名长度不能超过128位")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(128, ErrorMessage = "角色长度不能超过128位")]
    public string Role { get; set; } = "User";
}

public class UserUpdateDto
{
    [MaxLength(128, ErrorMessage = "用户名长度不能超过128位")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(128, ErrorMessage = "头像地址长度不能超过128位")]
    public string Avatar { get; set; } = string.Empty;

    [MaxLength(1024, ErrorMessage = "个人简介长度不能超过1024位")]
    public string Info { get; set; } = string.Empty;
}
