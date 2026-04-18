using MarketOurs.Data.DataModels;
using MarketOurs.DataAPI.Configs;
using MarketOurs.DataAPI.Repos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MarketOurs.DataAPI.Services.Background;

public class ReviewBackgroundService(
    ReviewMessageQueue queue,
    IServiceScopeFactory scopeFactory,
    NotificationMessageQueue notificationQueue,
    IMemoryCache memoryCache,
    IDistributedCache distributedCache,
    ILogger<ReviewBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ReviewBackgroundService is starting.");

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
                string notificationTargetId;
                string relatedPostId;

                if (message.Type == ReviewType.Post)
                {
                    var post = await postRepo.GetByIdAsync(message.TargetId);
                    if (post == null)
                    {
                        logger.LogWarning("Post {TargetId} not found when processing review queue", message.TargetId);
                        continue;
                    }

                    messageStr = $"title: {post.Title}, content: {post.Content}";
                    userId = post.UserId;
                    targetId = post.Id;
                    notificationTargetId = post.Id;
                    relatedPostId = post.Id;
                }
                else
                {
                    var comment = await commentRepo.GetByIdAsync(message.TargetId);
                    if (comment == null)
                    {
                        logger.LogWarning("Comment {TargetId} not found when processing review queue", message.TargetId);
                        continue;
                    }

                    messageStr = $"content: {comment.Content}";
                    userId = comment.UserId;
                    targetId = comment.Id;
                    notificationTargetId = comment.PostId;
                    relatedPostId = comment.PostId;
                }

                var reviewResult = await reviewService.Review(messageStr);
                var isApproved = string.IsNullOrWhiteSpace(reviewResult);

                if (message.Type == ReviewType.Post)
                {
                    await postRepo.SetReviewStatusAsync(message.TargetId, isApproved);
                    InvalidatePostCaches(targetId);
                }
                else
                {
                    await commentRepo.SetReviewStatusAsync(message.TargetId, isApproved);
                    InvalidateCommentCaches(targetId, relatedPostId);
                }

                notificationQueue.Enqueue(new NotificationMessage()
                {
                    UserId = userId,
                    Type = NotificationType.Review,
                    Content = isApproved ? "审核通过" : reviewResult,
                    TargetId = notificationTargetId,
                    Title = "审核信息"
                });

                logger.LogInformation(
                    "Finished reviewing {ReviewType} {TargetId}, approved: {IsApproved}",
                    message.Type,
                    message.TargetId,
                    isApproved);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while reviewing message {@Message}", message);
            }
        }

        logger.LogInformation("ReviewBackgroundService is stopping.");
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

    private void InvalidateCommentCaches(string commentId, string postId)
    {
        memoryCache.Remove(CacheKeys.CommentMem(commentId));
        _ = distributedCache.RemoveAsync(CacheKeys.CommentDist(commentId));
        memoryCache.Remove(CacheKeys.PostComments(postId));
    }
}
