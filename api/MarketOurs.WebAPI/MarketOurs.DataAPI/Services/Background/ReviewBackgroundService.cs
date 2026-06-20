using MarketOurs.Data.DataModels;
using MarketOurs.DataAPI.Configs;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services;
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

                var isPost = message.Type == ReviewType.Post;
                string name;

                if (isPost)
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
                    
                    name = post.Title;
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
                    
                    name = comment.Content;
                }

                var reviewResult = await reviewService.Review(messageStr);
                var isApproved = string.IsNullOrWhiteSpace(reviewResult);

                if (message.Type == ReviewType.Post)
                {
                    await postRepo.SetReviewStatusAsync(message.TargetId, isApproved);
                    InvalidatePostCaches(targetId);

                    // 审核不通过时删除帖子关联的图片
                    if (!isApproved)
                    {
                        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
                        await DeletePostImagesAsync(postRepo, storageService, message.TargetId);
                    }
                }
                else
                {
                    await commentRepo.SetReviewStatusAsync(message.TargetId, isApproved);
                    InvalidateCommentCaches(targetId, relatedPostId);
                }

                var a = isPost ? "帖子" : "评论";

                notificationQueue.Enqueue(new NotificationMessage()
                {
                    UserId = userId,
                    Type = NotificationType.Review,
                    Content = isApproved ? $"您的{a}: {name} 已通过" : reviewResult,
                    TargetId = notificationTargetId,
                    Title = "审核信息",
                    Params = new ReviewParams(
                        isPost ? "post" : "comment",
                        name,
                        isApproved,
                        isApproved ? null : reviewResult)
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

    private async Task DeletePostImagesAsync(IPostRepo postRepo, IStorageService storageService, string postId)
    {
        try
        {
            var post = await postRepo.GetByIdAsync(postId);
            if (post?.Images is { Count: > 0 })
            {
                var deleted = await storageService.DeleteFilesAsync(post.Images);
                logger.LogInformation("Review rejected: deleted {Count} images for post {PostId}", deleted, postId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete images for rejected post {PostId}", postId);
        }
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
