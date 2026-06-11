using MarketOurs.Data;
using MarketOurs.Data.DataModels;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Configs;
using MarketOurs.DataAPI.Exceptions;
using MarketOurs.DataAPI.Repos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MarketOurs.DataAPI.Services;

/// <summary>
/// 关注服务接口，处理用户关注和屏蔽逻辑
/// </summary>
public interface IFollowService
{
    /// <summary>
    /// 切换关注状态 (关注/取消关注)
    /// </summary>
    Task<FollowToggleResult> ToggleFollowAsync(string followerId, string followingId);

    /// <summary>
    /// 获取用户的粉丝列表
    /// </summary>
    Task<PagedResultDto<UserSimpleDto>> GetFollowersAsync(string userId, PaginationParams @params);

    /// <summary>
    /// 获取用户的关注列表
    /// </summary>
    Task<PagedResultDto<UserSimpleDto>> GetFollowingAsync(string userId, PaginationParams @params);

    /// <summary>
    /// 获取关注统计数据
    /// </summary>
    Task<FollowStatsDto> GetFollowStatsAsync(string userId, string? viewerUserId = null);

    /// <summary>
    /// 屏蔽用户
    /// </summary>
    Task<bool> BlockUserAsync(string blockerId, string blockedId);

    /// <summary>
    /// 取消屏蔽用户
    /// </summary>
    Task<bool> UnblockUserAsync(string blockerId, string blockedId);

    /// <summary>
    /// 获取被屏蔽的用户ID列表（包括双向屏蔽）
    /// </summary>
    Task<List<string>> GetBlockedUserIdsAsync(string userId);

    /// <summary>
    /// 获取当前用户屏蔽的用户列表（分页）
    /// </summary>
    Task<PagedResultDto<UserSimpleDto>> GetBlockedUsersAsync(string userId, PaginationParams @params);
}

public class FollowService(
    IUserRepo userRepo,
    IDbContextFactory<MarketContext> factory,
    IEnumerable<IConnectionMultiplexer> redisEnumerable,
    ILockService lockService,
    IMemoryCache memoryCache,
    ILogger<FollowService> logger) : IFollowService
{
    private readonly IConnectionMultiplexer? _redis = redisEnumerable.FirstOrDefault();

    // 关系集合从数据库重建后的过期时间。
    // 取较短值是为了给 cache-aside 的并发竞态兜底：万一“读重建”与“写删除”交错导致集合短暂残缺，
    // 也会在该时间内自动失效并重新从数据库重建，而不会长期脏读。
    private static readonly TimeSpan SetRebuildTtl = TimeSpan.FromMinutes(30);

    public async Task<FollowToggleResult> ToggleFollowAsync(string followerId, string followingId)
    {
        // 检查是否屏蔽关系
        var isBlocked = await IsBlockedAsync(followerId, followingId);
        var isBlockedBy = await IsBlockedAsync(followingId, followerId);

        if (isBlocked || isBlockedBy)
        {
            throw new BusinessException(ErrorCode.CannotFollowBlockedUser, "无法关注已屏蔽或屏蔽您的用户");
        }

        // 使用分布式锁防止并发
        var lockKey = $"lock:follow:{followerId}:{followingId}";
        var lockValue = Guid.NewGuid().ToString();
        var acquired = await lockService.AcquireAsync(lockKey, lockValue, TimeSpan.FromSeconds(5));

        if (!acquired)
        {
            throw new BusinessException(ErrorCode.FollowTooFrequent, "操作过于频繁，请稍后重试");
        }

        try
        {
            await using var context = await factory.CreateDbContextAsync();
            var follower = await context.Users
                .Include(u => u.Following)
                .FirstOrDefaultAsync(u => u.Id == followerId);

            var following = await context.Users
                .Include(u => u.Followers)
                .FirstOrDefaultAsync(u => u.Id == followingId);

            if (follower == null || following == null)
            {
                throw new ResourceAccessException(ErrorCode.UserNotFound, "用户不存在");
            }

            bool isFollowing;
            if (follower.Following.Any(u => u.Id == followingId))
            {
                // 取消关注
                follower.Following.Remove(following);
                isFollowing = false;
            }
            else
            {
                // 添加关注
                follower.Following.Add(following);
                isFollowing = true;
            }

            await context.SaveChangesAsync();

            // 缓存失效（cache-aside）：删除受影响的关系集合，下次读取时从数据库全量重建。
            // 不在这里做增量 SetAdd/SetRemove —— 增量维护要求集合始终等于数据库全集，
            // 但冷启动 / TTL 过期 / 屏蔽路径都会打破该前提，导致集合残缺、计数错乱。
            if (_redis != null)
            {
                var db = _redis.GetDatabase();
                await db.KeyDeleteAsync(CacheKeys.UserFollowing(followerId));
                await db.KeyDeleteAsync(CacheKeys.UserFollowers(followingId));
            }

            // 清除统计缓存
            memoryCache.Remove(CacheKeys.FollowStats(followerId));
            memoryCache.Remove(CacheKeys.FollowStats(followingId));

            var followerCount = await GetFollowerCountAsync(followingId);
            var followingCount = await GetFollowingCountAsync(followerId);

            return new FollowToggleResult(isFollowing, followerCount, followingCount);
        }
        finally
        {
            await lockService.ReleaseAsync(lockKey, lockValue);
        }
    }

    public async Task<PagedResultDto<UserSimpleDto>> GetFollowersAsync(string userId, PaginationParams @params)
    {
        var followers = await userRepo.GetFollowersAsync(userId, @params.PageIndex, @params.PageSize);
        var totalCount = await userRepo.GetFollowerCountAsync(userId);

        var items = followers?.Select(MapToSimpleDto).ToList() ?? [];

        return PagedResultDto<UserSimpleDto>.Success(items, totalCount, @params.PageIndex, @params.PageSize);
    }

    public async Task<PagedResultDto<UserSimpleDto>> GetFollowingAsync(string userId, PaginationParams @params)
    {
        var following = await userRepo.GetFollowingAsync(userId, @params.PageIndex, @params.PageSize);
        var totalCount = await userRepo.GetFollowingCountAsync(userId);

        var items = following?.Select(MapToSimpleDto).ToList() ?? [];

        return PagedResultDto<UserSimpleDto>.Success(items, totalCount, @params.PageIndex, @params.PageSize);
    }

    public async Task<FollowStatsDto> GetFollowStatsAsync(string userId, string? viewerUserId = null)
    {
        var followerCount = await GetFollowerCountAsync(userId);
        var followingCount = await GetFollowingCountAsync(userId);

        var stats = new FollowStatsDto
        {
            FollowerCount = followerCount,
            FollowingCount = followingCount
        };

        if (!string.IsNullOrEmpty(viewerUserId) && viewerUserId != userId)
        {
            stats.IsFollowing = await IsFollowingAsync(viewerUserId, userId);
            stats.IsFollowedBy = await IsFollowingAsync(userId, viewerUserId);
            stats.IsBlocked = await IsBlockedAsync(viewerUserId, userId);
            stats.IsBlockedBy = await IsBlockedAsync(userId, viewerUserId);
        }

        return stats;
    }

    public async Task<bool> BlockUserAsync(string blockerId, string blockedId)
    {
        // 使用分布式锁
        var lockKey = $"lock:block:{blockerId}:{blockedId}";
        var lockValue = Guid.NewGuid().ToString();
        var acquired = await lockService.AcquireAsync(lockKey, lockValue, TimeSpan.FromSeconds(5));

        if (!acquired)
        {
            throw new BusinessException(ErrorCode.BlockTooFrequent, "操作过于频繁，请稍后重试");
        }

        try
        {
            await using var context = await factory.CreateDbContextAsync();
            var blocker = await context.Users
                .Include(u => u.BlockedUsers)
                .Include(u => u.Following)
                .FirstOrDefaultAsync(u => u.Id == blockerId);

            var blocked = await context.Users
                .Include(u => u.Following)
                .FirstOrDefaultAsync(u => u.Id == blockedId);

            if (blocker == null || blocked == null)
            {
                throw new ResourceAccessException(ErrorCode.UserNotFound, "用户不存在");
            }

            // 添加屏蔽关系
            if (!blocker.BlockedUsers.Any(u => u.Id == blockedId))
            {
                blocker.BlockedUsers.Add(blocked);
            }

            // 自动解除双向关注
            if (blocker.Following.Any(u => u.Id == blockedId))
            {
                blocker.Following.Remove(blocked);
            }
            if (blocked.Following.Any(u => u.Id == blockerId))
            {
                blocked.Following.Remove(blocker);
            }

            await context.SaveChangesAsync();

            // 更新 Redis 缓存
            if (_redis != null)
            {
                var db = _redis.GetDatabase();

                // cache-aside：删除受影响的关系集合，下次读取时从数据库全量重建。
                // 屏蔽会在数据库层解除双向关注，因此必须失效双方的 following 和 followers 集合，
                // 以及屏蔽者的 blocked 集合。旧实现对 followers 漏清、对 blocked 做增量 SetAdd，
                // 都会留下与数据库不一致的残缺集合。
                await db.KeyDeleteAsync(CacheKeys.UserBlocked(blockerId));
                await db.KeyDeleteAsync(CacheKeys.UserFollowing(blockerId));
                await db.KeyDeleteAsync(CacheKeys.UserFollowing(blockedId));
                await db.KeyDeleteAsync(CacheKeys.UserFollowers(blockerId));
                await db.KeyDeleteAsync(CacheKeys.UserFollowers(blockedId));
            }

            // 清除统计缓存
            memoryCache.Remove(CacheKeys.FollowStats(blockerId));
            memoryCache.Remove(CacheKeys.FollowStats(blockedId));

            return true;
        }
        finally
        {
            await lockService.ReleaseAsync(lockKey, lockValue);
        }
    }

    public async Task<bool> UnblockUserAsync(string blockerId, string blockedId)
    {
        await using var context = await factory.CreateDbContextAsync();
        var blocker = await context.Users
            .Include(u => u.BlockedUsers)
            .FirstOrDefaultAsync(u => u.Id == blockerId);

        var blocked = await context.Users.FirstOrDefaultAsync(u => u.Id == blockedId);

        if (blocker == null || blocked == null)
        {
            throw new ResourceAccessException(ErrorCode.UserNotFound, "用户不存在");
        }

        if (blocker.BlockedUsers.Any(u => u.Id == blockedId))
        {
            blocker.BlockedUsers.Remove(blocked);
            await context.SaveChangesAsync();
        }

        // 缓存失效（cache-aside）：删除整个屏蔽集合，下次读取时从数据库全量重建。
        if (_redis != null)
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(CacheKeys.UserBlocked(blockerId));
        }

        return true;
    }

    public async Task<List<string>> GetBlockedUserIdsAsync(string userId)
    {
        return await userRepo.GetBlockedUserIdsAsync(userId);
    }

    public async Task<PagedResultDto<UserSimpleDto>> GetBlockedUsersAsync(string userId, PaginationParams @params)
    {
        var blocked = await userRepo.GetBlockedUsersAsync(userId);
        var total = blocked?.Count ?? 0;
        var items = (blocked ?? [])
            .Skip((@params.PageIndex - 1) * @params.PageSize)
            .Take(@params.PageSize)
            .Select(MapToSimpleDto)
            .ToList();

        return PagedResultDto<UserSimpleDto>.Success(items, total, @params.PageIndex, @params.PageSize);
    }

    // 私有辅助方法
    private async Task<int> GetFollowerCountAsync(string userId)
    {
        if (_redis != null)
        {
            var members = await GetRelationSetAsync(
                CacheKeys.UserFollowers(userId),
                () => userRepo.GetFollowerIdsAsync(userId));
            return members.Length;
        }

        return await userRepo.GetFollowerCountAsync(userId);
    }

    private async Task<int> GetFollowingCountAsync(string userId)
    {
        if (_redis != null)
        {
            var members = await GetRelationSetAsync(
                CacheKeys.UserFollowing(userId),
                () => userRepo.GetFollowingIdsAsync(userId));
            return members.Length;
        }

        return await userRepo.GetFollowingCountAsync(userId);
    }

    private async Task<bool> IsFollowingAsync(string followerId, string followingId)
    {
        if (_redis != null)
        {
            var members = await GetRelationSetAsync(
                CacheKeys.UserFollowing(followerId),
                () => userRepo.GetFollowingIdsAsync(followerId));
            return members.Any(m => m == followingId);
        }

        return await userRepo.IsFollowingAsync(followerId, followingId);
    }

    /// <summary>
    /// 读取关系集合（cache-aside）。命中则直接返回；未命中则从数据库全量加载、写回 Redis 并设置较短 TTL。
    /// 关键不变量：Redis 中的关系集合要么是数据库的“完整镜像”，要么不存在 —— 绝不允许存在“残缺集合”，
    /// 因此 SetLength / 成员判断才可信。写操作（关注/取关/屏蔽）只删除受影响的 key，不做增量修改。
    /// </summary>
    private async Task<RedisValue[]> GetRelationSetAsync(string key, Func<Task<List<string>>> loadFromDb)
    {
        var db = _redis!.GetDatabase();

        if (await db.KeyExistsAsync(key))
        {
            return await db.SetMembersAsync(key);
        }

        var ids = await loadFromDb();
        if (ids.Count == 0)
        {
            // 数据库中也没有任何关系：不缓存空集合（避免占用 key），直接返回空。
            return [];
        }

        var values = ids.Select(id => (RedisValue)id).ToArray();
        await db.SetAddAsync(key, values);
        await db.KeyExpireAsync(key, SetRebuildTtl);
        return values;
    }

    private async Task<bool> IsBlockedAsync(string blockerId, string blockedId)
    {
        if (_redis != null)
        {
            var members = await GetRelationSetAsync(
                CacheKeys.UserBlocked(blockerId),
                () => userRepo.GetBlockedUserIdsByMeAsync(blockerId));
            return members.Any(m => m == blockedId);
        }

        return await userRepo.IsBlockedAsync(blockerId, blockedId);
    }

    private static UserSimpleDto MapToSimpleDto(UserModel user)
    {
        return new UserSimpleDto
        {
            Id = user.Id,
            Name = user.Name,
            Avatar = user.Avatar
        };
    }
}
