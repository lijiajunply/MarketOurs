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
    private static readonly TimeSpan RelationshipTtl = TimeSpan.FromDays(7);
    private static readonly TimeSpan StatsTtl = TimeSpan.FromHours(1);

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

            // 更新 Redis 缓存
            if (_redis != null)
            {
                var db = _redis.GetDatabase();
                var followingKey = CacheKeys.UserFollowing(followerId);
                var followersKey = CacheKeys.UserFollowers(followingId);

                if (isFollowing)
                {
                    await db.SetAddAsync(followingKey, followingId);
                    await db.SetAddAsync(followersKey, followerId);
                }
                else
                {
                    await db.SetRemoveAsync(followingKey, followingId);
                    await db.SetRemoveAsync(followersKey, followerId);
                }

                await db.KeyExpireAsync(followingKey, RelationshipTtl);
                await db.KeyExpireAsync(followersKey, RelationshipTtl);
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
                await db.SetAddAsync(CacheKeys.UserBlocked(blockerId), blockedId);
                await db.KeyExpireAsync(CacheKeys.UserBlocked(blockerId), RelationshipTtl);

                // 清除关注缓存
                await db.SetRemoveAsync(CacheKeys.UserFollowing(blockerId), blockedId);
                await db.SetRemoveAsync(CacheKeys.UserFollowing(blockedId), blockerId);
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

        // 更新 Redis 缓存
        if (_redis != null)
        {
            var db = _redis.GetDatabase();
            await db.SetRemoveAsync(CacheKeys.UserBlocked(blockerId), blockedId);
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
            var db = _redis.GetDatabase();
            var key = CacheKeys.UserFollowers(userId);
            var count = await db.SetLengthAsync(key);
            if (count > 0) return (int)count;
        }

        return await userRepo.GetFollowerCountAsync(userId);
    }

    private async Task<int> GetFollowingCountAsync(string userId)
    {
        if (_redis != null)
        {
            var db = _redis.GetDatabase();
            var key = CacheKeys.UserFollowing(userId);
            var count = await db.SetLengthAsync(key);
            if (count > 0) return (int)count;
        }

        return await userRepo.GetFollowingCountAsync(userId);
    }

    private async Task<bool> IsFollowingAsync(string followerId, string followingId)
    {
        if (_redis != null)
        {
            var db = _redis.GetDatabase();
            var key = CacheKeys.UserFollowing(followerId);
            if (await db.KeyExistsAsync(key))
            {
                return await db.SetContainsAsync(key, followingId);
            }
        }

        return await userRepo.IsFollowingAsync(followerId, followingId);
    }

    private async Task<bool> IsBlockedAsync(string blockerId, string blockedId)
    {
        if (_redis != null)
        {
            var db = _redis.GetDatabase();
            var key = CacheKeys.UserBlocked(blockerId);
            if (await db.KeyExistsAsync(key))
            {
                return await db.SetContainsAsync(key, blockedId);
            }
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
