using MarketOurs.DataAPI.Configs;
using MarketOurs.DataAPI.Exceptions;
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
    Task<LikeToggleResult> SetPostLikeAsync(string postId, string userId);

    /// <summary>
    /// 切换帖子点踩状态
    /// </summary>
    Task<LikeToggleResult> SetPostDislikeAsync(string postId, string userId);

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
    Task<LikeToggleResult> SetCommentLikeAsync(string commentId, string userId);

    /// <summary>
    /// 切换评论点踩状态
    /// </summary>
    Task<LikeToggleResult> SetCommentDislikeAsync(string commentId, string userId);
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
    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(7);

    /// <summary>
    /// Lua 脚本：原子化点赞/点踩切换
    /// KEYS[1] = primaryKey, KEYS[2] = oppositeKey
    /// ARGV[1] = userId, ARGV[2] = expirySeconds
    /// 返回: [isMember (1=已添加, 0=已移除), primaryCount, oppositeCount, oppositeRemoved (1=已从对方列表移除)]
    /// </summary>
    private const string ToggleScript = """
        local is_member = redis.call('SISMEMBER', KEYS[1], ARGV[1])
        if is_member == 1 then
            redis.call('SREM', KEYS[1], ARGV[1])
            return {0, redis.call('SCARD', KEYS[1]), redis.call('SCARD', KEYS[2]), 0}
        else
            local opposite_removed = redis.call('SREM', KEYS[2], ARGV[1])
            redis.call('SADD', KEYS[1], ARGV[1])
            redis.call('EXPIRE', KEYS[1], ARGV[2])
            return {1, redis.call('SCARD', KEYS[1]), redis.call('SCARD', KEYS[2]), opposite_removed}
        end
        """;

    /// <inheritdoc/>
    public async Task<int> GetPostLikesAsync(string postId, int fallbackCount) =>
        await GetCountAsync(CacheKeys.PostLikes(postId), fallbackCount);

    /// <inheritdoc/>
    public async Task<int> GetPostDislikesAsync(string postId, int fallbackCount) =>
        await GetCountAsync(CacheKeys.PostDislikes(postId), fallbackCount);

    /// <inheritdoc/>
    public async Task<int> GetCommentLikesAsync(string commentId, int fallbackCount) =>
        await GetCountAsync(CacheKeys.CommentLikes(commentId), fallbackCount);

    /// <inheritdoc/>
    public async Task<int> GetCommentDislikesAsync(string commentId, int fallbackCount) =>
        await GetCountAsync(CacheKeys.CommentDislikes(commentId), fallbackCount);

    /// <inheritdoc/>
    public async Task<LikeToggleResult> SetPostLikeAsync(string postId, string userId) =>
        await ToggleActionAsync(TargetType.Post, ActionType.Like, postId, userId,
            () => postRepo.GetLikeUsersAsync(postId),
            () => postRepo.GetDislikeUsersAsync(postId));

    /// <inheritdoc/>
    public async Task<LikeToggleResult> SetPostDislikeAsync(string postId, string userId) =>
        await ToggleActionAsync(TargetType.Post, ActionType.Dislike, postId, userId,
            () => postRepo.GetDislikeUsersAsync(postId),
            () => postRepo.GetLikeUsersAsync(postId));

    /// <inheritdoc/>
    public async Task<LikeToggleResult> SetCommentLikeAsync(string commentId, string userId) =>
        await ToggleActionAsync(TargetType.Comment, ActionType.Like, commentId, userId,
            () => commentRepo.GetLikeUsersAsync(commentId),
            () => commentRepo.GetDislikeUsersAsync(commentId));

    /// <inheritdoc/>
    public async Task<LikeToggleResult> SetCommentDislikeAsync(string commentId, string userId) =>
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

    private async Task<LikeToggleResult> ToggleActionAsync(
        TargetType target,
        ActionType action,
        string targetId,
        string userId,
        Func<Task<List<UserModel>?>> primaryDbFetcher,
        Func<Task<List<UserModel>?>> oppositeDbFetcher)
    {
        var lockKey = $"lock:like:{targetId}:{userId}";
        var lockValue = Guid.NewGuid().ToString();

        if (!await lockService.AcquireAsync(lockKey, lockValue, TimeSpan.FromSeconds(5)))
        {
            logger.LogWarning("Failed to acquire lock for user {UserId} on {Target} {TargetId}", userId, target, targetId);
            throw new ResourceAccessException(ErrorCode.TooManyRequests, "操作过于频繁，请稍后再试");
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
                await queue.EnqueueAsync(new LikeMessage(target, action, targetId, userId));
                return new LikeToggleResult(isLike, !isLike, 0, 0);
            }

            var db = _redis.GetDatabase();

            // Step 1: 确保缓存已从 DB 回填
            await EnsureCacheAsync(db, primaryKey, primaryDbFetcher);
            await EnsureCacheAsync(db, oppositeKey, oppositeDbFetcher);

            // Step 2: 原子化执行切换 (Lua 脚本确保 SetContains + SetAdd/SetRemove 的原子性)
            var expirySeconds = (int)CacheTtl.TotalSeconds;
            var result = await db.ScriptEvaluateAsync(
                ToggleScript,
                new RedisKey[] { primaryKey, oppositeKey },
                new RedisValue[] { userId, expirySeconds }
            );

            var values = (RedisResult[])result!;
            var isMember = (int)values[0] == 1;
            var primaryCount = (int)values[1];
            var oppositeCount = (int)values[2];
            var oppositeRemoved = (int)values[3] == 1;

            // Step 3: 根据原子操作结果入队正确的消息
            if (isMember)
            {
                // 切换为 ON：添加到 primary，如果之前存在 opposite 则需要先取消
                if (oppositeRemoved)
                    await queue.EnqueueAsync(new LikeMessage(target, oppositeCancelAction, targetId, userId));
                await queue.EnqueueAsync(new LikeMessage(target, action, targetId, userId));
            }
            else
            {
                // 切换为 OFF：从 primary 移除
                await queue.EnqueueAsync(new LikeMessage(target, cancelAction, targetId, userId));
            }

            var likeCount = isLike ? primaryCount : oppositeCount;
            var dislikeCount = isLike ? oppositeCount : primaryCount;

            return new LikeToggleResult(
                IsLiked: isLike ? isMember : false,
                IsDisliked: isLike ? false : isMember,
                LikeCount: likeCount,
                DislikeCount: dislikeCount
            );
        }
        catch (Exception ex) when (ex is not ResourceAccessException)
        {
            // Redis 操作失败时，记录日志并回退到仅 DB 模式
            // 注意：不再盲目入队 action，避免在 toggle-off 路径中产生重复点赞
            logger.LogWarning(ex, "Redis error toggling {Action} for {Target} {TargetId}, falling back to DB-only", action, target, targetId);

            // DB 层的 SetLikesAsync/DeleteLikesAsync 有幂等性保护，所以仅入队原始 action 是安全的
            // 如果 Redis 全部不可用，依赖 DB 层做去重
            await queue.EnqueueAsync(new LikeMessage(target, action, targetId, userId));

            return new LikeToggleResult(
                IsLiked: action == ActionType.Like,
                IsDisliked: action == ActionType.Dislike,
                LikeCount: 0,
                DislikeCount: 0
            );
        }
        finally
        {
            await lockService.ReleaseAsync(lockKey, lockValue);
        }
    }

    /// <summary>
    /// 确保 Redis 中存在指定 key 的缓存数据，如不存在则从 DB 回填。
    /// 同时刷新已有 key 的过期时间，防止 key 永久存在。
    /// </summary>
    private async Task EnsureCacheAsync(IDatabase db, string key, Func<Task<List<UserModel>?>> dbFetcher)
    {
        try
        {
            if (await db.KeyExistsAsync(key))
            {
                // 刷新已有 key 的 TTL，防止无过期时间的 key 永久残留
                await db.KeyExpireAsync(key, CacheTtl);
                return;
            }

            var users = await dbFetcher();
            if (users is { Count: > 0 })
            {
                var redisValues = users.Select(u => (RedisValue)u.Id).ToArray();
                await db.SetAddAsync(key, redisValues);
                await db.KeyExpireAsync(key, CacheTtl);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to ensure Redis cache for key {Key}", key);
            // 缓存回填失败不阻断流程，后续原子操作会在空集合上执行
        }
    }
}
