using MarketOurs.Data.DataModels;
using MarketOurs.DataAPI.Repos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MarketOurs.DataAPI.Services.Background;

/// <summary>
/// 异步通知发送后台服务
/// 负责消费通知队列，处理站内信持久化、邮件发送及移动端 Push 发送的综合逻辑
/// </summary>
public class NotificationSyncBackgroundService(
    NotificationMessageQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationSyncBackgroundService> logger) : BackgroundService
{
    /// <inheritdoc/>
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
                    var pushService = scope.ServiceProvider.GetRequiredService<IPushService>();

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

                    var user = await userRepo.GetByIdAsync(message.UserId);
                    if (user != null)
                    {
                        // 2. 检查是否需要发送邮件
                        var settings = await notificationService.GetPushSettingsAsync(message.UserId);
                        if (settings.EnableEmailNotifications)
                        {
                            if (!string.IsNullOrEmpty(user.Email) && user.IsEmailVerified)
                            {
                                await emailService.SendEmailAsync(user.Email, message.Title, message.Content);
                                logger.LogInformation("Sent email notification to {Email}", user.Email);
                            }
                        }

                        // 3. 发送移动端推送 (如果有 PushToken)
                        if (!string.IsNullOrEmpty(user.PushToken))
                        {
                            bool shouldPush = message.Type switch
                            {
                                NotificationType.CommentReply or NotificationType.PostReply => settings.EnableCommentReplyPush,
                                NotificationType.HotList => settings.EnableHotListPush,
                                _ => true // 默认系统通知始终推送
                            };

                            if (shouldPush)
                            {
                                var data = new Dictionary<string, string>
                                {
                                    ["type"] = message.Type.ToString(),
                                    ["targetId"] = message.TargetId ?? ""
                                };
                                await pushService.SendPushNotificationAsync(user.PushToken, message.Title, message.Content, data);
                                logger.LogInformation("Sent mobile push notification to user {UserId}", message.UserId);
                            }
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
