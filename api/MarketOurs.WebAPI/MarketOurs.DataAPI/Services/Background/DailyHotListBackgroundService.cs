using MarketOurs.Data.DataModels;
using MarketOurs.DataAPI.Repos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MarketOurs.DataAPI.Services.Background;

/// <summary>
/// 每日热门榜单定时推送后台服务
/// 每天早晨 8 点扫描前 5 名热门帖子并向所有活跃用户发送站内通知
/// </summary>
public class DailyHotListBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<DailyHotListBackgroundService> logger) : BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("DailyHotListBackgroundService is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            // 设定每天早晨 8 点推送
            var targetTime = new DateTime(now.Year, now.Month, now.Day, 8, 0, 0);
            if (now > targetTime)
            {
                targetTime = targetTime.AddDays(1);
            }

            var delay = targetTime - now;
            logger.LogInformation("Next hot list push at {TargetTime}, delay: {Delay}", targetTime, delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            // 执行推送逻辑
            await PushHotListAsync();
        }

        logger.LogInformation("DailyHotListBackgroundService is stopping.");
    }

    private async Task PushHotListAsync()
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var postRepo = scope.ServiceProvider.GetRequiredService<IPostRepo>();
            var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepo>();
            var notificationQueue = scope.ServiceProvider.GetRequiredService<NotificationMessageQueue>();

            // 1. 获取热榜（前5名）
            var hotPosts = await postRepo.GetHotAsync(5);
            if (hotPosts.Count == 0) return;

            var hotTitles = string.Join("\n", hotPosts.Select((p, i) => $"{i + 1}. {p.Title}"));
            var title = "🔥 今日校园热榜";
            var content = $"来看看大家都在聊什么：\n{hotTitles}";

            // 2. 获取所有活跃用户
            var users = await userRepo.GetAllAsync(1, 1000); // 简单起见，这里假设用户量不大，实际应分页处理

            foreach (var user in users)
            {
                // 这里可以进一步检查用户的推送设置，但为了简化，直接入队，由 NotificationSyncService 统一处理
                notificationQueue.Enqueue(new NotificationMessage
                {
                    UserId = user.Id,
                    Title = title,
                    Content = content,
                    Type = NotificationType.HotList
                });
            }

            logger.LogInformation("Enqueued hot list notifications for {Count} users", users.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error pushing daily hot list");
        }
    }
}
