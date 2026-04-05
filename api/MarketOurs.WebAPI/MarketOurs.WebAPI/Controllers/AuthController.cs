using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;

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
    public async Task<ApiResponse<TokenDto>> Login([FromBody] LoginRequest request)
    {
        logger.LogInformation("用户尝试登录: {Account}", request.Account);
        var token = await loginService.Login(request.Account, request.Password, request.DeviceType);
        return ApiResponse<TokenDto>.Success(token, "登录成功");
    }

    /// <summary>
    /// 用户注册
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ApiResponse<UserDto>> Register([FromBody] UserCreateDto request)
    {
        logger.LogInformation("新用户注册: {Account}", request.Account);
        var user = await userService.CreateAsync(request);
        return ApiResponse<UserDto>.Success(user, "注册成功");
    }

    /// <summary>
    /// 刷新令牌
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ApiResponse<TokenDto>> Refresh([FromBody] RefreshRequest request)
    {
        logger.LogInformation("刷新令牌, 设备类型: {DeviceType}", request.DeviceType);
        var token = await loginService.Login(request.RefreshToken, request.DeviceType);
        return string.IsNullOrEmpty(token.AccessToken)
            ? ApiResponse<TokenDto>.Fail(401, "刷新令牌无效或已过期")
            : ApiResponse<TokenDto>.Success(token, "刷新成功");
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
    /// 验证手机号
    /// </summary>
    [HttpPost("verify-phone")]
    [AllowAnonymous]
    public async Task<ApiResponse> VerifyPhone([FromBody] VerifyCodeRequest request)
    {
        var result = await userService.VerifyPhoneCodeAsync(request.Code);
        return result
            ? ApiResponse.Success("手机号验证成功，账号已激活")
            : ApiResponse.Fail(400, "验证码无效或已过期");
    }

    /// <summary>
    /// 忘记密码 - 发送重置码
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ApiResponse> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await userService.ForgotPasswordAsync(request.Account);
        // 处于安全考虑，无论账号是否存在都返回相同消息 (或者简单点直接告诉用户)
        return result
            ? ApiResponse.Success("重置密码验证码已发送至您的邮箱或手机")
            : ApiResponse.Fail(404, "该账号未注册");
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
    /// 重新发送验证邮件或短信 (根据账号)
    /// </summary>
    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public async Task<ApiResponse> ResendVerification([FromBody] ForgotPasswordRequest request)
    {
        var user = await userService.GetByAccountAsync(request.Account);
        if (user == null) return ApiResponse.Fail(404, "用户不存在");
        if (user.IsActive) return ApiResponse.Fail(400, "账号已激活");

        if (!string.IsNullOrEmpty(user.Email) && request.Account.Contains('@'))
        {
            await userService.SendVerificationEmailAsync(user.Id);
            return ApiResponse.Success("激活邮件已重新发送");
        }

        if (!string.IsNullOrEmpty(user.Phone))
        {
            await userService.SendPhoneVerificationCodeAsync(user.Id);
            return ApiResponse.Success("激活短信已重新发送");
        }

        return ApiResponse.Fail(400, "无法发送验证码");
    }

    /// <summary>
    /// 发送当前账号的邮箱验证码 (需登录)
    /// </summary>
    [HttpPost("send-email-code")]
    [Authorize]
    public async Task<ApiResponse> SendEmailCode()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return ApiResponse.Fail(401, "未授权");

        var result = await userService.SendVerificationEmailAsync(userId);
        return result
            ? ApiResponse.Success("验证码已发送至您的邮箱")
            : ApiResponse.Fail(500, "发送失败，请稍后重试");
    }

    /// <summary>
    /// 发送当前账号的手机验证码 (需登录)
    /// </summary>
    [HttpPost("send-phone-code")]
    [Authorize]
    public async Task<ApiResponse> SendPhoneCode()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return ApiResponse.Fail(401, "未授权");

        var result = await userService.SendPhoneVerificationCodeAsync(userId);
        return result
            ? ApiResponse.Success("验证码已发送至您的手机")
            : ApiResponse.Fail(500, "发送失败，请稍后重试");
    }

    /// <summary>
    /// 校验当前账号的邮箱验证码 (需登录)
    /// </summary>
    [HttpPost("verify-email-code")]
    [Authorize]
    public async Task<ApiResponse> VerifyEmailCode([FromBody] VerifyCodeRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return ApiResponse.Fail(401, "未授权");

        var result = await userService.VerifyEmailAsync(request.Code);
        return result
            ? ApiResponse.Success("邮箱验证成功")
            : ApiResponse.Fail(400, "验证码无效或已过期");
    }

    /// <summary>
    /// 第三方登录入口
    /// </summary>
    [HttpGet("external-login")]
    [AllowAnonymous]
    public IActionResult ExternalLogin([FromQuery] string provider, [FromQuery] string returnUrl = "/")
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Auth", new { returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, provider);
    }

    /// <summary>
    /// 第三方登录回调
    /// </summary>
    [HttpGet("external-login-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback([FromQuery] string returnUrl = "/",
        [FromQuery] string? remoteError = null)
    {
        if (remoteError != null)
        {
            return Redirect($"{returnUrl}?error={Uri.EscapeDataString(remoteError)}");
        }

        var result = await HttpContext.AuthenticateAsync("OAuth2");
        if (!result.Succeeded || result.Principal == null)
        {
            return Redirect($"{returnUrl}?error={Uri.EscapeDataString("认证失败")}");
        }

        var email = result.Principal.FindFirstValue(ClaimTypes.Email);
        var name = result.Principal.FindFirstValue(ClaimTypes.Name) ?? email?.Split('@')[0];
        var avatar = result.Principal.FindFirstValue("urn:github:avatar")
                     ?? result.Principal.FindFirstValue("image")
                     ?? string.Empty;

        if (string.IsNullOrEmpty(email))
        {
            var nameIdentifier = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(nameIdentifier))
            {
                return Redirect($"{returnUrl}?error={Uri.EscapeDataString("无法获取用户邮箱信息")}");
            }

            email = $"{nameIdentifier}@external.local";
        }

        try
        {
            var token = await loginService.LoginWithOAuthAsync(email, name ?? "User", avatar, "Web");

            // 登录成功后注销外部cookie，保持状态清晰
            await HttpContext.SignOutAsync("OAuth2");

            return Redirect($"{returnUrl}?accessToken={Uri.EscapeDataString(token.AccessToken)}&refreshToken={Uri.EscapeDataString(token.RefreshToken)}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "第三方登录失败");
            return Redirect($"{returnUrl}?error={Uri.EscapeDataString("登录处理失败")}");
        }
    }
}

public class LoginRequest
{
    [Required(ErrorMessage = "账号不能为空")] 
    [MaxLength(128, ErrorMessage = "账号长度不能超过128位")]
    public string Account { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")] 
    [MaxLength(128, ErrorMessage = "密码长度不能超过128位")]
    public string Password { get; set; } = string.Empty;

    public string DeviceType { get; set; } = "Web";
}

public class RefreshRequest
{
    [Required(ErrorMessage = "刷新令牌不能为空")] 
    public string RefreshToken { get; set; } = string.Empty;

    public string DeviceType { get; set; } = "Web";
}

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "账号不能为空")] 
    [MaxLength(128, ErrorMessage = "账号长度不能超过128位")]
    public string Account { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "验证码不能为空")] 
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "新密码不能为空")] 
    [MinLength(6, ErrorMessage = "新密码长度不能少于6位")] 
    [MaxLength(128, ErrorMessage = "新密码长度不能超过128位")]
    public string NewPassword { get; set; } = string.Empty;
}

public class VerifyCodeRequest
{
    [Required(ErrorMessage = "验证码不能为空")] 
    public string Code { get; set; } = string.Empty;
}