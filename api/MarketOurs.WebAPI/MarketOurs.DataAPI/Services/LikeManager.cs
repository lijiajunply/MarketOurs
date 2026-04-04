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
    IEnumerable<IConnectionMultiplexer> redisEnumerable, // Inject IEnumerable to handle optional gracefully
    LikeMessageQueue queue,
    IPostRepo postRepo,
    ICommentRepo commentRepo,
    ILogger<LikeManager> logger) : ILikeManager
{
    private readonly IConnectionMultiplexer? _redis = redisEnumerable.FirstOrDefault();

    public async Task<int> GetPostLikesAsync(string postId, int fallbackCount) =>
        await GetCountAsync($"post:{postId}:likes", fallbackCount);

    public async Task<int> GetPostDislikesAsync(string postId, int fallbackCount) =>
        await GetCountAsync($"post:{postId}:dislikes", fallbackCount);

    public async Task<int> GetCommentLikesAsync(string commentId, int fallbackCount) =>
        await GetCountAsync($"comment:{commentId}:likes", fallbackCount);

    public async Task<int> GetCommentDislikesAsync(string commentId, int fallbackCount) =>
        await GetCountAsync($"comment:{commentId}:dislikes", fallbackCount);

    public async Task SetPostLikeAsync(string postId, string userId) =>
        await ProcessActionAsync($"post:{postId}:likes", userId, new LikeMessage(TargetType.Post, ActionType.Like, postId, userId), () => postRepo.GetLikeUsersAsync(postId));

    public async Task SetPostDislikeAsync(string postId, string userId) =>
        await ProcessActionAsync($"post:{postId}:dislikes", userId, new LikeMessage(TargetType.Post, ActionType.Dislike, postId, userId), () => postRepo.GetDislikeUsersAsync(postId));

    public async Task SetCommentLikeAsync(string commentId, string userId) =>
        await ProcessActionAsync($"comment:{commentId}:likes", userId, new LikeMessage(TargetType.Comment, ActionType.Like, commentId, userId), () => commentRepo.GetLikeUsersAsync(commentId));

    public async Task SetCommentDislikeAsync(string commentId, string userId) =>
        await ProcessActionAsync($"comment:{commentId}:dislikes", userId, new LikeMessage(TargetType.Comment, ActionType.Dislike, commentId, userId), () => commentRepo.GetDislikeUsersAsync(commentId));

    
    /// <summary>
    /// 查看Count
    /// </summary>
    /// <param name="key"></param>
    /// <param name="fallbackCount"></param>
    /// <returns></returns>
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
            logger.LogWarning(ex, "Failed to read count from Redis for key {Key}. Falling back to DB count.", key);
        }

        return fallbackCount;
    }

    
    /// <summary>
    /// 添加数据
    /// </summary>
    /// <param name="key"></param>
    /// <param name="userId"></param>
    /// <param name="message"></param>
    /// <param name="dbFetcher"></param>
    private async Task ProcessActionAsync(string key, string userId, LikeMessage message, Func<Task<List<UserModel>?>> dbFetcher)
    {
        if (_redis == null)
        {
            // If no Redis, just queue to DB background worker immediately
            await queue.EnqueueAsync(message);
            return;
        }

        try
        {
            var db = _redis.GetDatabase();

            // Ensure cache is hot before modifying
            if (!await db.KeyExistsAsync(key))
            {
                var users = await dbFetcher();
                if (users is { Count: > 0 })
                {
                    var redisValues = users.Select(u => (RedisValue)u.Id).ToArray();
                    await db.SetAddAsync(key, redisValues);
                }
            }

            var added = await db.SetAddAsync(key, userId);
            if (added)
            {
                // Give it a 7-day TTL if it's inactive
                await db.KeyExpireAsync(key, TimeSpan.FromDays(7));
                await queue.EnqueueAsync(message);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to write to Redis for key {Key}. Queueing anyway.", key);
            await queue.EnqueueAsync(message);
        }
    }
}
