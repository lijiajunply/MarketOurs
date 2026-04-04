using MarketOurs.DataAPI.Configs;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services.Background;
using MarketOurs.Data.DataModels;

namespace MarketOurs.DataAPI.Services;

public interface ILikeManager
{
    // Post
    Task<int> GetPostLikesAsync(string postId, int fallbackCount);
    Task<int> GetPostDislikesAsync(string postId, int fallbackCount);
    Task SetPostLikeAsync(string postId, string userId);
    Task SetPostDislikeAsync(string postId, string userId);

    // Comment
    Task<int> GetCommentLikesAsync(string commentId, int fallbackCount);
    Task<int> GetCommentDislikesAsync(string commentId, int fallbackCount);
    Task SetCommentLikeAsync(string commentId, string userId);
    Task SetCommentDislikeAsync(string commentId, string userId);
}

public class LikeManager(
    IEnumerable<IConnectionMultiplexer> redisEnumerable,
    LikeMessageQueue queue,
    IPostRepo postRepo,
    ICommentRepo commentRepo,
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

        try
        {
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
