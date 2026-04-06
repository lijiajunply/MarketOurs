using MarketOurs.Data.DataModels;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Repos;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MarketOurs.DataAPI.Services;

/// <summary>
/// 通知服务接口，处理系统通知、站内信以及用户推送设置
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// 获取用户的所有通知列表 (分页)
    /// </summary>
    Task<PagedResultDto<NotificationDto>> GetUserNotificationsAsync(string userId, PaginationParams @params);

    /// <summary>
    /// 获取用户的未读通知总数
    /// </summary>
    Task<int> GetUnreadCountAsync(string userId);

    /// <summary>
    /// 创建并持久化一条新通知
    /// </summary>
    Task CreateNotificationAsync(NotificationModel notification);

    /// <summary>
    /// 将指定通知标记为已读
    /// </summary>
    Task MarkAsReadAsync(string id);

    /// <summary>
    /// 将用户的所有通知一键标记为已读
    /// </summary>
    Task MarkAllAsReadAsync(string userId);

    /// <summary>
    /// 获取用户的推送偏好设置 (如是否接收邮件、APP推送等)
    /// </summary>
    Task<PushSettingsDto> GetPushSettingsAsync(string userId);

    /// <summary>
    /// 更新用户的推送偏好设置
    /// </summary>
    Task UpdatePushSettingsAsync(string userId, PushSettingsDto settings);
}

public class NotificationService(
    INotificationRepo notificationRepo,
    IUserRepo userRepo,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task<PagedResultDto<NotificationDto>> GetUserNotificationsAsync(string userId, PaginationParams @params)
    {
        var totalCount = await notificationRepo.CountAsync(userId);
        var notifications = await notificationRepo.GetUserNotificationsAsync(userId, @params.PageIndex, @params.PageSize);
        var dtos = notifications.Select(MapToDto).ToList();
        return PagedResultDto<NotificationDto>.Success(dtos, totalCount, @params.PageIndex, @params.PageSize);
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await notificationRepo.GetUserUnreadCountAsync(userId);
    }

    public async Task CreateNotificationAsync(NotificationModel notification)
    {
        await notificationRepo.CreateAsync(notification);
        logger.LogInformation("Created notification for user {UserId}, Type: {Type}", notification.UserId, notification.Type);
    }

    public async Task MarkAsReadAsync(string id)
    {
        var notification = await notificationRepo.GetByIdAsync(id);
        if (notification != null)
        {
            notification.IsRead = true;
            await notificationRepo.UpdateAsync(notification);
        }
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        await notificationRepo.MarkAllAsReadAsync(userId);
    }

    public async Task<PushSettingsDto> GetPushSettingsAsync(string userId)
    {
        var user = await userRepo.GetByIdAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.PushSettings))
        {
            return new PushSettingsDto();
        }

        try
        {
            return JsonSerializer.Deserialize<PushSettingsDto>(user.PushSettings) ?? new PushSettingsDto();
        }
        catch
        {
            return new PushSettingsDto();
        }
    }

    /// <inheritdoc/>
    public async Task UpdatePushSettingsAsync(string userId, PushSettingsDto settings)
    {
        var user = await userRepo.GetByIdAsync(userId);
        if (user != null)
        {
            user.PushSettings = JsonSerializer.Serialize(settings);
            await userRepo.UpdateAsync(user);
        }
    }

    private static NotificationDto MapToDto(NotificationModel model)
    {
        return new NotificationDto
        {
            Id = model.Id,
            UserId = model.UserId,
            Title = model.Title,
            Content = model.Content,
            Type = model.Type,
            TargetId = model.TargetId,
            IsRead = model.IsRead,
            CreatedAt = model.CreatedAt
        };
    }
}
