using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketOurs.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController(ILoginService loginService, IUserService userService, ILogger<AuthController> logger)
    : ControllerBase
{
    /// <summary>
    /// 用户登录
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ApiResponse<string>> Login([FromBody] LoginRequest request)
    {
        logger.LogInformation("用户尝试登录: {Email}", request.Email);
        var token = await loginService.Login(request.Email, request.Password, request.DeviceType);
        return ApiResponse<string>.Success(token, "登录成功");
    }

    /// <summary>
    /// 用户注册
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ApiResponse<UserDto>> Register([FromBody] UserCreateDto request)
    {
        logger.LogInformation("新用户注册: {Email}", request.Email);
        var user = await userService.CreateAsync(request);
        return ApiResponse<UserDto>.Success(user, "注册成功");
    }

    /// <summary>
    /// 刷新令牌
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ApiResponse<string>> Refresh([FromBody] RefreshRequest request)
    {
        logger.LogInformation("刷新令牌, 设备类型: {DeviceType}", request.DeviceType);
        var token = await loginService.Login(request.RefreshToken, request.DeviceType);
        if (string.IsNullOrEmpty(token))
        {
            return ApiResponse<string>.Fail(401, "刷新令牌无效或已过期");
        }
        return ApiResponse<string>.Success(token, "刷新成功");
    }

    /// <summary>
    /// 注销登录
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ApiResponse> Logout([FromQuery] string deviceType = "Web")
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return ApiResponse.Fail(401, "未授权");
        }
        
        logger.LogInformation("用户注销: {UserId}, 设备类型: {DeviceType}", userId, deviceType);
        var result = await loginService.Logout(userId, deviceType);
        return result 
            ? ApiResponse.Success("注销成功") 
            : ApiResponse.Fail(500, "注销失败");
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    [HttpGet("info")]
    [Authorize]
    public async Task<ApiResponse<UserDto>> GetUserInfo()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return ApiResponse<UserDto>.Fail(401, "未授权");
        }

        var user = await userService.GetByIdAsync(userId);
        if (user == null)
        {
            return ApiResponse<UserDto>.Fail(404, "用户不存在");
        }

        return ApiResponse<UserDto>.Success(user, "获取用户信息成功");
    }
}

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public string DeviceType { get; set; } = "Web";
}

public class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;

    public string DeviceType { get; set; } = "Web";
}
