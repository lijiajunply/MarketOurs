using System;
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
    [Required] public string Account { get; set; } = string.Empty;
    
    [Required] public string Password { get; set; } = string.Empty;
    [Required] public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
}

public class UserUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public string Info { get; set; } = string.Empty;
}
