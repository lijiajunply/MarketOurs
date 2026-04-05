using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MarketOurs.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class NotificationController(INotificationService notificationService) : ControllerBase
{
    /// <summary>
    /// 获取当前用户的通知列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponse<PagedResultDto<NotificationDto>>> GetNotifications([FromQuery] PaginationParams @params)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return ApiResponse<PagedResultDto<NotificationDto>>.Fail(401, "未授权");

        var result = await notificationService.GetUserNotificationsAsync(userId, @params);
        return ApiResponse<PagedResultDto<NotificationDto>>.Success(result, "获取通知成功");
    }

    /// <summary>
    /// 获取未读通知数量
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<ApiResponse<int>> GetUnreadCount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return ApiResponse<int>.Fail(401, "未授权");

        var count = await notificationService.GetUnreadCountAsync(userId);
        return ApiResponse<int>.Success(count, "获取未读数成功");
    }

    /// <summary>
    /// 标记通知为已读
    /// </summary>
    [HttpPost("{id}/read")]
    public async Task<ApiResponse> MarkAsRead(string id)
    {
        await notificationService.MarkAsReadAsync(id);
        return ApiResponse.Success("操作成功");
    }

    /// <summary>
    /// 标记所有通知为已读
    /// </summary>
    [HttpPost("read-all")]
    public async Task<ApiResponse> MarkAllAsRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return ApiResponse.Fail(401, "未授权");

        await notificationService.MarkAllAsReadAsync(userId);
        return ApiResponse.Success("操作成功");
    }

    /// <summary>
    /// 获取通知设置
    /// </summary>
    [HttpGet("settings")]
    public async Task<ApiResponse<PushSettingsDto>> GetSettings()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return ApiResponse<PushSettingsDto>.Fail(401, "未授权");

        var settings = await notificationService.GetPushSettingsAsync(userId);
        return ApiResponse<PushSettingsDto>.Success(settings, "获取设置成功");
    }

    /// <summary>
    /// 更新通知设置
    /// </summary>
    [HttpPut("settings")]
    public async Task<ApiResponse> UpdateSettings([FromBody] PushSettingsDto settings)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return ApiResponse.Fail(401, "未授权");

        await notificationService.UpdatePushSettingsAsync(userId, settings);
        return ApiResponse.Success("更新成功");
    }
}
