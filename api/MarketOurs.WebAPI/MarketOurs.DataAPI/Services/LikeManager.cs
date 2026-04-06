using MarketOurs.DataAPI.Configs;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services.Background;
using MarketOurs.Data.DataModels;

namespace MarketOurs.DataAPI.Services;

/// <summary>
/// 点赞管理接口，处理帖子和评论的点赞/点踩逻辑，支持 Redis 缓存与异步持久化
/// </summary>
public interface ILikeManager
{
    // --- 帖子相关 ---

    /// <summary>
    /// 获取帖子点赞数 (优先从 Redis 获取，否则使用 fallback)
    /// </summary>
    Task<int> GetPostLikesAsync(string postId, int fallbackCount);

    /// <summary>
    /// 获取帖子点踩数
    /// </summary>
    Task<int> GetPostDislikesAsync(string postId, int fallbackCount);

    /// <summary>
    /// 切换帖子点赞状态 (如果已点赞则取消，如果已点踩则先取消点踩再点赞)
    /// </summary>
    Task SetPostLikeAsync(string postId, string userId);

    /// <summary>
    /// 切换帖子点踩状态
    /// </summary>
    Task SetPostDislikeAsync(string postId, string userId);

    // --- 评论相关 ---

    /// <summary>
    /// 获取评论点赞数
    /// </summary>
    Task<int> GetCommentLikesAsync(string commentId, int fallbackCount);

    /// <summary>
    /// 获取评论点踩数
    /// </summary>
    Task<int> GetCommentDislikesAsync(string commentId, int fallbackCount);

    /// <summary>
    /// 切换评论点赞状态
    /// </summary>
    Task SetCommentLikeAsync(string commentId, string userId);

    /// <summary>
    /// 切换评论点踩状态
    /// </summary>
    Task SetCommentDislikeAsync(string commentId, string userId);
}

public class LikeManager(
    IEnumerable<IConnectionMultiplexer> redisEnumerable,
    LikeMessageQueue queue,
    IPostRepo postRepo,
    ICommentRepo commentRepo,
    ILockService lockService,
    ILogger<LikeManager> logger) : ILikeManager
{
    private readonly IConnectionMultiplexer? _redis = redisEnumerable.FirstOrDefault();

    public async Task<int> GetPostLikesAsync(string postId, int fallbackCount) =>
        await GetCountAsync(CacheKeys.PostLikes(postId), fallbackCount);

    public async Task<int> GetPostDislikesAsync(string postId, int fallbackCount) =>
        await GetCountAsync(CacheKeys.PostDislikes(postId), fallbackCount);

    public async Task<int> GetCommentLikesAsync(string commentId, int fallbackCount) =>
        await GetCountAsync(CacheKeys.CommentLikes(commentId), fallbackCount);

    public async Task<int> GetCommentDislikesAsync(string commentId, int fallbackCount) =>
        await GetCountAsync(CacheKeys.CommentDislikes(commentId), fallbackCount);

    public async Task SetPostLikeAsync(string postId, string userId) =>
        await ToggleActionAsync(TargetType.Post, ActionType.Like, postId, userId, 
            () => postRepo.GetLikeUsersAsync(postId), 
            () => postRepo.GetDislikeUsersAsync(postId));

    public async Task SetPostDislikeAsync(string postId, string userId) =>
        await ToggleActionAsync(TargetType.Post, ActionType.Dislike, postId, userId, 
            () => postRepo.GetDislikeUsersAsync(postId), 
            () => postRepo.GetLikeUsersAsync(postId));

    public async Task SetCommentLikeAsync(string commentId, string userId) =>
        await ToggleActionAsync(TargetType.Comment, ActionType.Like, commentId, userId, 
            () => commentRepo.GetLikeUsersAsync(commentId), 
            () => commentRepo.GetDislikeUsersAsync(commentId));

    public async Task SetCommentDislikeAsync(string commentId, string userId) =>
        await ToggleActionAsync(TargetType.Comment, ActionType.Dislike, commentId, userId, 
            () => commentRepo.GetDislikeUsersAsync(commentId), 
            () => commentRepo.GetLikeUsersAsync(commentId));

    private async Task<int> GetCountAsync(string key, int fallbackCount)
    {
        if (_redis == null) return fallbackCount;
        try
        {
            var db = _redis.GetDatabase();
            if (await db.KeyExistsAsync(key))
            {
                return (int)await db.SetLengthAsync(key);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read count from Redis for key {Key}", key);
        }
        return fallbackCount;
    }

    private async Task ToggleActionAsync(
        TargetType target, 
        ActionType action, 
        string targetId, 
        string userId, 
        Func<Task<List<UserModel>?>> primaryDbFetcher,
        Func<Task<List<UserModel>?>> oppositeDbFetcher)
    {
        var lockKey = $"lock:like:{targetId}:{userId}";
        var lockValue = Guid.NewGuid().ToString();

        // 尝试获取分布式锁，防止用户连续快速点击
        if (!await lockService.AcquireAsync(lockKey, lockValue, TimeSpan.FromSeconds(5)))
        {
            logger.LogWarning("Failed to acquire lock for user {UserId} on {Target} {TargetId}", userId, target, targetId);
            return;
        }

        try
        {
            var isLike = action == ActionType.Like;
            
            var primaryKey = target == TargetType.Post 
                ? (isLike ? CacheKeys.PostLikes(targetId) : CacheKeys.PostDislikes(targetId))
                : (isLike ? CacheKeys.CommentLikes(targetId) : CacheKeys.CommentDislikes(targetId));
                
            var oppositeKey = target == TargetType.Post 
                ? (isLike ? CacheKeys.PostDislikes(targetId) : CacheKeys.PostLikes(targetId))
                : (isLike ? CacheKeys.CommentDislikes(targetId) : CacheKeys.CommentLikes(targetId));
            
            var cancelAction = isLike ? ActionType.Unlike : ActionType.Undislike;
            var oppositeCancelAction = isLike ? ActionType.Undislike : ActionType.Unlike;

            if (_redis == null)
            {
                // Fallback: just enqueue and let DB handle potential duplicates/mutual exclusivity
                await queue.EnqueueAsync(new LikeMessage(target, action, targetId, userId));
                return;
            }

            var db = _redis.GetDatabase();

            await EnsureCacheAsync(db, primaryKey, primaryDbFetcher);
            await EnsureCacheAsync(db, oppositeKey, oppositeDbFetcher);

            if (await db.SetContainsAsync(primaryKey, userId))
            {
                // Already has this action, so toggle it OFF
                await db.SetRemoveAsync(primaryKey, userId);
                await queue.EnqueueAsync(new LikeMessage(target, cancelAction, targetId, userId));
            }
            else
            {
                // Toggle ON
                // 1. Remove opposite if exists
                if (await db.SetRemoveAsync(oppositeKey, userId))
                {
                    await queue.EnqueueAsync(new LikeMessage(target, oppositeCancelAction, targetId, userId));
                }
                
                // 2. Add primary
                await db.SetAddAsync(primaryKey, userId);
                await db.KeyExpireAsync(primaryKey, TimeSpan.FromDays(7));
                await queue.EnqueueAsync(new LikeMessage(target, action, targetId, userId));
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error toggling {Action} for {Target} {TargetId}", action, target, targetId);
            await queue.EnqueueAsync(new LikeMessage(target, action, targetId, userId));
        }
        finally
        {
            await lockService.ReleaseAsync(lockKey, lockValue);
        }
    }

    private async Task EnsureCacheAsync(IDatabase db, string key, Func<Task<List<UserModel>?>> dbFetcher)
    {
        if (!await db.KeyExistsAsync(key))
        {
            var users = await dbFetcher();
            if (users is { Count: > 0 })
            {
                var redisValues = users.Select(u => (RedisValue)u.Id).ToArray();
                await db.SetAddAsync(key, redisValues);
                await db.KeyExpireAsync(key, TimeSpan.FromDays(7));
            }
        }
    }
}
