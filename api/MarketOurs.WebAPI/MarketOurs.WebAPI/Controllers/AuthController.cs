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
        return string.IsNullOrEmpty(token)
            ? ApiResponse<string>.Fail(401, "刷新令牌无效或已过期")
            : ApiResponse<string>.Success(token, "刷新成功");
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

    /// <summary>
    /// 验证邮箱
    /// </summary>
    [HttpGet("verify-email")]
    [AllowAnonymous]
    public async Task<ApiResponse> VerifyEmail([FromQuery] string token)
    {
        var result = await userService.VerifyEmailAsync(token);
        return result
            ? ApiResponse.Success("邮箱验证成功，账号已激活")
            : ApiResponse.Fail(400, "验证码无效或已过期");
    }

    /// <summary>
    /// 忘记密码 - 发送重置码
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ApiResponse> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await userService.ForgotPasswordAsync(request.Email);
        // 处于安全考虑，无论邮箱是否存在都返回相同消息 (或者简单点直接告诉用户)
        return result
            ? ApiResponse.Success("重置密码验证码已发送至您的邮箱")
            : ApiResponse.Fail(404, "该邮箱未注册");
    }

    /// <summary>
    /// 重置密码
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ApiResponse> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await userService.ResetPasswordAsync(request.Token, request.NewPassword);
        return result
            ? ApiResponse.Success("密码重置成功，请重新登录")
            : ApiResponse.Fail(400, "验证码无效或已过期");
    }

    /// <summary>
    /// 重新发送验证邮件
    /// </summary>
    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public async Task<ApiResponse> ResendVerification([FromBody] ForgotPasswordRequest request)
    {
        var user = await userService.GetByEmailAsync(request.Email);
        if (user == null) return ApiResponse.Fail(404, "用户不存在");
        if (user.IsActive) return ApiResponse.Fail(400, "账号已激活");

        await userService.SendVerificationEmailAsync(user.Id);
        return ApiResponse.Success("激活邮件已重新发送");
    }
}

public class LoginRequest
{
    [Required] [EmailAddress] public string Email { get; set; } = string.Empty;

    [Required] public string Password { get; set; } = string.Empty;

    public string DeviceType { get; set; } = "Web";
}

public class RefreshRequest
{
    [Required] public string RefreshToken { get; set; } = string.Empty;

    public string DeviceType { get; set; } = "Web";
}

public class ForgotPasswordRequest
{
    [Required] [EmailAddress] public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    [Required] public string Token { get; set; } = string.Empty;
    [Required] [MinLength(6)] public string NewPassword { get; set; } = string.Empty;
}