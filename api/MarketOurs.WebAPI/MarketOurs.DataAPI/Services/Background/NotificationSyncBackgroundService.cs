using MarketOurs.Data.DataModels;
using MarketOurs.DataAPI.Repos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MarketOurs.DataAPI.Services.Background;

public class NotificationSyncBackgroundService(
    NotificationMessageQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationSyncBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("NotificationSyncBackgroundService is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (queue.TryDequeue(out var message) && message != null)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                    var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepo>();
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                    // 1. 创建站内通知
                    var notification = new NotificationModel
                    {
                        UserId = message.UserId,
                        Title = message.Title,
                        Content = message.Content,
                        Type = message.Type,
                        TargetId = message.TargetId,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    await notificationService.CreateNotificationAsync(notification);

                    // 2. 检查是否需要发送邮件
                    var settings = await notificationService.GetPushSettingsAsync(message.UserId);
                    if (settings.EnableEmailNotifications)
                    {
                        var user = await userRepo.GetByIdAsync(message.UserId);
                        if (user != null && !string.IsNullOrEmpty(user.Email) && user.IsEmailVerified)
                        {
                            await emailService.SendEmailAsync(user.Email, message.Title, message.Content);
                            logger.LogInformation("Sent email notification to {Email}", user.Email);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing notification message for user {UserId}", message.UserId);
                }
            }
            else
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        logger.LogInformation("NotificationSyncBackgroundService is stopping.");
    }
}
