using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Services;

namespace MarketOurs.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController(
    IAdminService adminService) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<ActionResult<ApiResponse<AdminOverviewDto>>> GetOverview()
    {
        var overview = await adminService.GetOverviewAsync();
        return Ok(ApiResponse<AdminOverviewDto>.Success(overview, "获取管理概览成功"));
    }

    [HttpGet("settings")]
    public async Task<ActionResult<ApiResponse<AdminSettingsDto>>> GetSettings()
    {
        var settings = await adminService.GetSettingsAsync();
        return Ok(ApiResponse<AdminSettingsDto>.Success(settings, "获取系统设置成功"));
    }

    [HttpPut("settings")]
    public async Task<ActionResult<ApiResponse<AdminSettingsDto>>> UpdateSettings([FromBody] AdminSettingsDto request)
    {
        var settings = await adminService.UpdateSettingsAsync(request);
        return Ok(ApiResponse<AdminSettingsDto>.Success(settings, "更新系统设置成功"));
    }

    [HttpPut("users/{id}/status")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUserStatus(string id, [FromBody] UpdateUserStatusRequest request)
    {
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId == id && !request.IsActive)
        {
            return Ok(ApiResponse<UserDto>.Fail(400, "不能停用当前登录的管理员账号"));
        }

        var user = await adminService.UpdateUserStatusAsync(id, request.IsActive);
        if (user == null)
        {
            return Ok(ApiResponse<UserDto>.Fail(404, "用户不存在"));
        }

        return Ok(ApiResponse<UserDto>.Success(user, "更新用户状态成功"));
    }
}
