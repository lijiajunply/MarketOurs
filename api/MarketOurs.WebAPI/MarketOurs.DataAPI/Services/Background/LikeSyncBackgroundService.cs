using MarketOurs.DataAPI.Repos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MarketOurs.DataAPI.Services.Background;

public class LikeSyncBackgroundService(
    LikeMessageQueue queue,
    IServiceProvider serviceProvider,
    ILogger<LikeSyncBackgroundService> logger) : Microsoft.Extensions.Hosting.BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("LikeSyncBackgroundService is starting.");

        await foreach (var message in queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepo>();
                var postRepo = scope.ServiceProvider.GetRequiredService<IPostRepo>();
                var commentRepo = scope.ServiceProvider.GetRequiredService<ICommentRepo>();

                var user = await userRepo.GetByIdAsync(message.UserId);
                if (user == null)
                {
                    logger.LogWarning("User {UserId} not found when syncing {Action} for {Target} {TargetId}", 
                        message.UserId, message.Action, message.Target, message.TargetId);
                    continue;
                }

                if (message.Target == TargetType.Post)
                {
                    if (message.Action == ActionType.Like)
                    {
                        await postRepo.SetLikesAsync(user, message.TargetId);
                    }
                    else if (message.Action == ActionType.Dislike)
                    {
                        await postRepo.SetDislikesAsync(user, message.TargetId);
                    }
                    else if (message.Action == ActionType.Unlike)
                    {
                        await postRepo.DeleteLikesAsync(message.TargetId, user.Id);
                    }
                    else if (message.Action == ActionType.Undislike)
                    {
                        await postRepo.DeleteDislikesAsync(message.TargetId, user.Id);
                    }
                }
                else if (message.Target == TargetType.Comment)
                {
                    if (message.Action == ActionType.Like)
                    {
                        await commentRepo.SetLikesAsync(user, message.TargetId);
                    }
                    else if (message.Action == ActionType.Dislike)
                    {
                        await commentRepo.SetDislikesAsync(user, message.TargetId);
                    }
                    else if (message.Action == ActionType.Unlike)
                    {
                        await commentRepo.DeleteLikesAsync(message.TargetId, user.Id);
                    }
                    else if (message.Action == ActionType.Undislike)
                    {
                        await commentRepo.DeleteDislikesAsync(message.TargetId, user.Id);
                    }
                }
                
                logger.LogDebug("Successfully synced {Action} for {Target} {TargetId} by user {UserId}",
                    message.Action, message.Target, message.TargetId, message.UserId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred executing like/dislike sync logic for message {@Message}", message);
            }
        }
        
        logger.LogInformation("LikeSyncBackgroundService is stopping.");
    }
}
