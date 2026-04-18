using MarketOurs.Data.DataModels;
using MarketOurs.DataAPI.Configs;
using MarketOurs.DataAPI.Repos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MarketOurs.DataAPI.Services.Background;

public class PostReviewBackgroundService(
    PostReviewMessageQueue queue,
    IServiceScopeFactory scopeFactory,
    NotificationMessageQueue notificationQueue,
    IMemoryCache memoryCache,
    IDistributedCache distributedCache,
    ILogger<PostReviewBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PostReviewBackgroundService is starting.");

        await foreach (var message in queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var postRepo = scope.ServiceProvider.GetRequiredService<IPostRepo>();
                var commentRepo = scope.ServiceProvider.GetRequiredService<ICommentRepo>();
                var reviewService = scope.ServiceProvider.GetRequiredService<IReviewService>();

                string messageStr;
                string userId;
                string targetId;

                if (message.Type == ReviewType.Post)
                {
                    var post = await postRepo.GetByIdAsync(message.PostId);
                    if (post == null)
                    {
                        logger.LogWarning("Post {PostId} not found when processing review queue", message.PostId);
                        continue;
                    }

                    messageStr = $"title: {post.Title}, content: {post.Content}";
                    userId = post.UserId;
                    targetId = post.Id;
                }
                else
                {
                    var comment = await commentRepo.GetByIdAsync(message.PostId);
                    if (comment == null)
                    {
                        logger.LogWarning("comment {PostId} not found when processing review queue", message.PostId);
                        continue;
                    }

                    messageStr = $"content: {comment.Content}";
                    userId = comment.UserId;
                    targetId = comment.Id;
                }

                var reviewResult = await reviewService.Review(messageStr);
                var isApproved = string.IsNullOrWhiteSpace(reviewResult);

                await postRepo.SetReviewStatusAsync(message.PostId, isApproved);
                notificationQueue.Enqueue(new NotificationMessage()
                {
                    UserId = userId,
                    Type = NotificationType.Review,
                    Content = isApproved ? "审核通过" : reviewResult,
                    TargetId = targetId,
                    Title = "审核信息"
                });
                InvalidatePostCaches(message.PostId);

                logger.LogInformation(
                    "Finished reviewing post {PostId}, approved: {IsApproved}",
                    message.PostId,
                    isApproved);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while reviewing post message {@Message}", message);
            }
        }

        logger.LogInformation("PostReviewBackgroundService is stopping.");
    }

    private void InvalidatePostCaches(string postId)
    {
        memoryCache.Remove(CacheKeys.PostMem(postId));
        _ = distributedCache.RemoveAsync(CacheKeys.PostDist(postId));

        for (int i = 5; i <= 20; i += 5)
        {
            memoryCache.Remove(CacheKeys.HotPostsMem(i));
            _ = distributedCache.RemoveAsync(CacheKeys.HotPostsDist(i));
        }
    }
}