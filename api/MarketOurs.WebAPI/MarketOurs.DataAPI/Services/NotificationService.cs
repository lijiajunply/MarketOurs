using MarketOurs.Data.DataModels;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Repos;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MarketOurs.DataAPI.Services;

public interface INotificationService
{
    Task<PagedResultDto<NotificationDto>> GetUserNotificationsAsync(string userId, PaginationParams @params);
    Task<int> GetUnreadCountAsync(string userId);
    Task CreateNotificationAsync(NotificationModel notification);
    Task MarkAsReadAsync(string id);
    Task MarkAllAsReadAsync(string userId);
    Task<PushSettingsDto> GetPushSettingsAsync(string userId);
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
