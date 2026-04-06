using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MarketOurs.WebAPI.Controllers;

/// <summary>
/// 通知控制器，提供用户通知列表查询、未读数统计、已读状态标记及推送设置管理功能
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public class NotificationController(INotificationService notificationService) : ControllerBase
{
    /// <summary>
    /// 获取当前登录用户的通知列表 (支持分页)
    /// </summary>
    /// <param name="params">分页参数</param>
    /// <returns>分页后的通知列表</returns>
    [HttpGet]
    public async Task<ApiResponse<PagedResultDto<NotificationDto>>> GetNotifications([FromQuery] PaginationParams @params)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return ApiResponse<PagedResultDto<NotificationDto>>.Fail(401, "未授权");

        var result = await notificationService.GetUserNotificationsAsync(userId, @params);
        return ApiResponse<PagedResultDto<NotificationDto>>.Success(result, "获取通知成功");
    }

    /// <summary>
    /// 获取当前登录用户的未读通知总数
    /// </summary>
    /// <returns>未读通知数量</returns>
    [HttpGet("unread-count")]
    public async Task<ApiResponse<int>> GetUnreadCount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return ApiResponse<int>.Fail(401, "未授权");

        var count = await notificationService.GetUnreadCountAsync(userId);
        return ApiResponse<int>.Success(count, "获取未读数成功");
    }

    /// <summary>
    /// 将指定的通知标记为已读
    /// </summary>
    /// <param name="id">通知唯一标识</param>
    /// <returns>操作结果描述</returns>
    [HttpPost("{id}/read")]
    public async Task<ApiResponse> MarkAsRead(string id)
    {
        await notificationService.MarkAsReadAsync(id);
        return ApiResponse.Success("操作成功");
    }

    /// <summary>
    /// 将当前用户的所有通知一键标记为已读
    /// </summary>
    /// <returns>操作结果描述</returns>
    [HttpPost("read-all")]
    public async Task<ApiResponse> MarkAllAsRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return ApiResponse.Fail(401, "未授权");

        await notificationService.MarkAllAsReadAsync(userId);
        return ApiResponse.Success("操作成功");
    }

    /// <summary>
    /// 获取当前用户的个性化推送设置 (如邮件通知、热门推送等)
    /// </summary>
    /// <returns>推送设置详情</returns>
    [HttpGet("settings")]
    public async Task<ApiResponse<PushSettingsDto>> GetSettings()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return ApiResponse<PushSettingsDto>.Fail(401, "未授权");

        var settings = await notificationService.GetPushSettingsAsync(userId);
        return ApiResponse<PushSettingsDto>.Success(settings, "获取设置成功");
    }

    /// <summary>
    /// 更新当前用户的个性化推送设置
    /// </summary>
    /// <param name="settings">推送设置请求对象</param>
    /// <returns>操作结果描述</returns>
    [HttpPut("settings")]
    public async Task<ApiResponse> UpdateSettings([FromBody] PushSettingsDto settings)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return ApiResponse.Fail(401, "未授权");

        await notificationService.UpdatePushSettingsAsync(userId, settings);
        return ApiResponse.Success("更新成功");
    }
}
