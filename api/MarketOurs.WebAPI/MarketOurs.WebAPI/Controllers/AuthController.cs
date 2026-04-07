using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Exceptions;
using MarketOurs.DataAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;

namespace MarketOurs.WebAPI.Controllers;

/// <summary>
/// 认证控制器，处理用户登录、注册、令牌刷新、第三方 OAuth 登录及账户验证逻辑
/// </summary>
[ApiController]
[Route("[controller]")]
public class AuthController(ILoginService loginService, IUserService userService, ILogger<AuthController> logger)
    : ControllerBase
{
    /// <summary>
    /// 标准账号密码登录
    /// </summary>
    /// <param name="request">包含账号、密码和设备类型的登录请求</param>
    /// <returns>包含双 Token 的成功结果</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<TokenDto>>> Login([FromBody] LoginRequest request)
    {
        try
        {
            logger.LogInformation("用户尝试登录: {Account}", request.Account);
            var token = await loginService.Login(request.Account, request.Password, request.DeviceType);
            return ApiResponse<TokenDto>.Success(token, "登录成功");
        }
        catch (AuthException e)
        {
            return Unauthorized(ApiResponse<TokenDto>.Fail(ErrorCode.Unauthorized, e.Message));
        }
    }

    /// <summary>
    /// 发送登录验证码 (免密码登录)
    /// </summary>
    /// <param name="request">包含账号的请求对象</param>
    [HttpPost("send-login-code")]
    [AllowAnonymous]
    public async Task<ApiResponse> SendLoginCode([FromBody] SendCodeRequest request)
    {
        logger.LogInformation("用户请求登录验证码: {Account}", request.Account);
        var result = await loginService.SendLoginCodeAsync(request.Account);
        return result
            ? ApiResponse.Success("验证码已发送")
            : ApiResponse.Fail(500, "发送失败，请稍后重试");
    }

    /// <summary>
    /// 使用验证码登录 (免密码登录)
    /// </summary>
    /// <param name="request">包含账号、验证码和设备类型的请求对象</param>
    [HttpPost("login-by-code")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<TokenDto>>> LoginByCode([FromBody] LoginByCodeRequest request)
    {
        try
        {
            logger.LogInformation("用户通过验证码尝试登录: {Account}", request.Account);
            var token = await loginService.LoginByCodeAsync(request.Account, request.Code, request.DeviceType);
            return ApiResponse<TokenDto>.Success(token, "登录成功");
        }
        catch (AuthException e)
        {
            return Unauthorized(ApiResponse<TokenDto>.Fail(ErrorCode.Unauthorized, e.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "验证码登录失败");
            return StatusCode(500, ApiResponse<TokenDto>.Fail(500, "登录处理失败"));
        }
    }

    /// <summary>
    /// 用户注册：第一步，提交信息获取注册令牌
    /// </summary>
    /// <param name="request">用户注册请求对象</param>
    /// <returns>注册令牌</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ApiResponse<string>> Register([FromBody] UserCreateDto request)
    {
        logger.LogInformation("新用户发起注册: {Account}", request.Account);
        var regToken = await loginService.RegisterUserAsync(request);
        return ApiResponse<string>.Success(regToken, "请继续进行验证码验证");
    }

    /// <summary>
    /// 用户注册：第二步，发送验证码
    /// </summary>
    /// <param name="regToken">注册令牌</param>
    [HttpPost("send-registration-code")]
    [AllowAnonymous]
    public async Task<ApiResponse> SendRegistrationCode([FromQuery] string regToken)
    {
        var result = await loginService.SendRegistrationCodeAsync(regToken);
        return result
            ? ApiResponse.Success("验证码已发送")
            : ApiResponse.Fail(500, "发送失败");
    }

    /// <summary>
    /// 用户注册：第三步，验证并完成注册
    /// </summary>
    [HttpPost("verify-registration")]
    [AllowAnonymous]
    public async Task<ApiResponse<UserDto>> VerifyRegistration([FromBody] VerifyRegistrationRequest request)
    {
        var user = await loginService.VerifyAndRegisterAsync(request.RegistrationToken, request.Code);
        return ApiResponse<UserDto>.Success(user, "注册成功");
    }

    /// <summary>
    /// 使用刷新令牌获取新的访问令牌
    /// </summary>
    /// <param name="request">包含刷新令牌的请求对象</param>
    /// <returns>新签发的令牌对</returns>
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
    /// 注销当前会话 (需要登录)
    /// </summary>
    /// <param name="deviceType">设备类型 (Web/Mobile)</param>
    /// <returns>操作结果描述</returns>
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
    /// 获取当前已登录用户的详细信息
    /// </summary>
    /// <returns>当前用户信息</returns>
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
    /// 验证邮箱激活令牌
    /// </summary>
    /// <param name="token">注册时需要的验证内容</param>
    /// <param name="code">邮件中的验证令牌</param>
    /// <returns>验证结果描述</returns>
    [HttpGet("verify-email")]
    [AllowAnonymous]
    public async Task<ApiResponse> VerifyEmail([FromQuery] string token, string code)
    {
        var result = await userService.VerifyEmailAsync(code);
        return result
            ? ApiResponse.Success("邮箱验证成功，账号已激活")
            : ApiResponse.Fail(400, "验证码无效或已过期");
    }

    /// <summary>
    /// 验证手机激活码
    /// </summary>
    /// <param name="token">注册时需要的验证内容</param>
    /// <param name="code">邮件中的验证令牌</param>
    /// <returns>验证结果描述</returns>
    [HttpPost("verify-phone")]
    [AllowAnonymous]
    public async Task<ApiResponse> VerifyPhone([FromQuery] string token, string code)
    {
        var result = await userService.VerifyPhoneCodeAsync(code);
        return result
            ? ApiResponse.Success("手机号验证成功，账号已激活")
            : ApiResponse.Fail(400, "验证码无效或已过期");
    }

    /// <summary>
    /// 忘记密码：根据账号发送重置验证码
    /// </summary>
    /// <param name="request">包含账号的请求对象</param>
    /// <returns>操作结果描述</returns>
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
    /// 重置密码：根据重置令牌设置新密码
    /// </summary>
    /// <param name="request">包含令牌和新密码的请求对象</param>
    /// <returns>操作结果描述</returns>
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
    /// 重新向未激活账户发送验证邮件或短信
    /// </summary>
    /// <param name="request">包含账号的请求对象</param>
    /// <returns>操作结果描述</returns>
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
    /// 向当前登录用户的邮箱发送验证码
    /// </summary>
    /// <returns>操作结果描述</returns>
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
    /// 向当前登录用户的手机号发送验证码
    /// </summary>
    /// <returns>操作结果描述</returns>
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
    /// 校验当前登录用户的邮箱验证码
    /// </summary>
    /// <param name="request">包含验证码的请求对象</param>
    /// <returns>操作结果描述</returns>
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
    /// 第三方登录/绑定统一入口
    /// </summary>
    /// <param name="schemeProvider">用于校验提供商名称并查找认证方案的提供商</param>
    /// <param name="provider">登录提供方 (如 GitHub, Google, Weixin)</param>
    /// <param name="returnUrl">登录成功后的回跳地址</param>
    /// <param name="purpose">用途: login 或 bind</param>
    /// <returns>跳转至第三方平台的 Challenge 响应</returns>
    [HttpGet("external-login")]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLogin([FromServices] IAuthenticationSchemeProvider schemeProvider, [FromQuery] string provider, [FromQuery] string returnUrl = "/", [FromQuery] string purpose = "login")
    {
        var scheme = (await schemeProvider.GetAllSchemesAsync())
            .FirstOrDefault(s => string.Equals(s.Name, provider, StringComparison.OrdinalIgnoreCase));

        if (scheme == null)
        {
            return BadRequest(ApiResponse.Fail(400, $"不支持的第三方登录: {provider}"));
        }

        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Auth", new { returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        properties.Items["purpose"] = purpose;
        
        // 如果是绑定，需要确保持有当前用户 ID
        if (purpose == "bind" && User.Identity?.IsAuthenticated == true)
        {
            properties.Items["userId"] = User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
        
        return Challenge(properties, scheme.Name);
    }

    /// <summary>
    /// 第三方登录成功后的内部回调处理
    /// </summary>
    /// <param name="returnUrl">最终回跳地址</param>
    /// <param name="remoteError">远程错误信息 (如有)</param>
    /// <returns>重定向至前端页面，并带上 AccessToken</returns>
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

        var purpose = result.Properties?.Items["purpose"] ?? "login";
        var userIdForBind = result.Properties?.Items.ContainsKey("userId") == true ? result.Properties.Items["userId"] : null;

        var provider = result.Properties?.Items[".AuthScheme"] ?? "Unknown";
        var providerId = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = result.Principal.FindFirstValue(ClaimTypes.Email);
        var name = result.Principal.FindFirstValue(ClaimTypes.Name) ?? email?.Split('@')[0];
        var avatar = result.Principal.FindFirstValue("urn:github:avatar")
                     ?? result.Principal.FindFirstValue("image")
                     ?? string.Empty;

        if (string.IsNullOrEmpty(providerId))
        {
            return Redirect($"{returnUrl}?error={Uri.EscapeDataString("无法获取第三方用户信息")}");
        }

        if (string.IsNullOrEmpty(email))
        {
            email = $"{providerId}@external.local";
        }

        try
        {
            if (purpose == "bind" && !string.IsNullOrEmpty(userIdForBind))
            {
                await loginService.BindThirdPartyAsync(userIdForBind, provider, providerId);
                await HttpContext.SignOutAsync("OAuth2");
                return Redirect($"{returnUrl}?message={Uri.EscapeDataString("绑定成功")}");
            }

            var token = await loginService.LoginWithOAuthAsync(provider, providerId, email, name ?? "User", avatar, "Web");

            // 登录成功后注销外部cookie，保持状态清晰
            await HttpContext.SignOutAsync("OAuth2");

            return Redirect(
                $"{returnUrl}?accessToken={Uri.EscapeDataString(token.AccessToken)}&refreshToken={Uri.EscapeDataString(token.RefreshToken)}");
        }
        catch (AuthException ex)
        {
            logger.LogWarning("第三方登录认证失败: {Message}", ex.Message);
            return Redirect($"{returnUrl}?error={Uri.EscapeDataString(ex.Message)}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "第三方登录失败");
            return Redirect($"{returnUrl}?error={Uri.EscapeDataString("登录处理失败")}");
        }
    }
}

/// <summary>
/// 登录请求对象
/// </summary>
public class LoginRequest
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
    [MaxLength(128, ErrorMessage = "密码长度不能超过128位")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 设备类型 (如 Web, Mobile)
    /// </summary>
    public string DeviceType { get; set; } = "Web";
}

/// <summary>
/// 刷新令牌请求对象
/// </summary>
public class RefreshRequest
{
    /// <summary>
    /// 刷新令牌 (RefreshToken)
    /// </summary>
    [Required(ErrorMessage = "刷新令牌不能为空")]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// 设备类型
    /// </summary>
    public string DeviceType { get; set; } = "Web";
}

/// <summary>
/// 忘记密码请求对象
/// </summary>
public class ForgotPasswordRequest
{
    /// <summary>
    /// 账号 (邮箱或手机号)
    /// </summary>
    [Required(ErrorMessage = "账号不能为空")]
    [MaxLength(128, ErrorMessage = "账号长度不能超过128位")]
    public string Account { get; set; } = string.Empty;
}

/// <summary>
/// 重置密码请求对象
/// </summary>
public class ResetPasswordRequest
{
    /// <summary>
    /// 验证码或重置令牌
    /// </summary>
    [Required(ErrorMessage = "验证码不能为空")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 新密码
    /// </summary>
    [Required(ErrorMessage = "新密码不能为空")]
    [MinLength(6, ErrorMessage = "新密码长度不能少于6位")]
    [MaxLength(128, ErrorMessage = "新密码长度不能超过128位")]
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// 注册验证请求对象
/// </summary>
public class VerifyRegistrationRequest
{
    /// <summary>
    /// 注册令牌 (从注册接口获取)
    /// </summary>
    [Required(ErrorMessage = "注册令牌不能为空")]
    public string RegistrationToken { get; set; } = string.Empty;

    /// <summary>
    /// 验证码
    /// </summary>
    [Required(ErrorMessage = "验证码不能为空")]
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// 验证码校验请求对象
/// </summary>
public class VerifyCodeRequest
{
    /// <summary>
    /// 验证码
    /// </summary>
    [Required(ErrorMessage = "验证码不能为空")]
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// 发送验证码请求
/// </summary>
public class SendCodeRequest
{
    /// <summary>
    /// 账号 (邮箱或手机号)
    /// </summary>
    [Required(ErrorMessage = "账号不能为空")]
    public string Account { get; set; } = string.Empty;
}

/// <summary>
/// 验证码登录请求
/// </summary>
public class LoginByCodeRequest
{
    /// <summary>
    /// 账号 (邮箱或手机号)
    /// </summary>
    [Required(ErrorMessage = "账号不能为空")]
    public string Account { get; set; } = string.Empty;

    /// <summary>
    /// 验证码
    /// </summary>
    [Required(ErrorMessage = "验证码不能为空")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 设备类型 (Web/Mobile)
    /// </summary>
    public string DeviceType { get; set; } = "Web";
}