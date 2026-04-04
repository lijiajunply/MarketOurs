using System.Text.Json;
using MarketOurs.Data.DataModels;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Repos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MarketOurs.DataAPI.Services;

public interface IPostService
{
    Task<List<PostDto>> GetAllAsync();
    Task<List<PostDto>> GetHotAsync(int count = 10);
    Task<PostDto?> GetByIdAsync(string id);
    Task<PostDto?> CreateAsync(PostCreateDto createDto);
    Task<PostDto?> UpdateAsync(string id, PostUpdateDto updateDto);
    Task DeleteAsync(string id);
    Task IncrementWatchAsync(string id);
    Task SetLikesAsync(string userId, string postId);
    Task SetDislikesAsync(string userId, string postId);
    Task<List<CommentDto>> GetCommentsAsync(string id, string type);
}

public class PostService(
    IPostRepo postRepo,
    IUserRepo userRepo,
    ILikeManager likeManager,
    IDistributedCache distributedCache,
    IMemoryCache memoryCache,
    IEnumerable<IConnectionMultiplexer> redisEnumerable,
    ILogger<PostService> logger) : IPostService
{
    private readonly IConnectionMultiplexer? _redis = redisEnumerable.FirstOrDefault();

    // 缓存过期时间配置
    private static readonly TimeSpan LocalHotCacheTtl = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan LocalPostCacheTtl = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan DistHotCacheTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DistPostCacheTtl = TimeSpan.FromMinutes(10);
    private const int WatchSyncThreshold = 10; // 多少次浏览量后同步一次数据库

    public async Task<List<PostDto>> GetAllAsync()
    {
        var posts = await postRepo.GetAllAsync();
        var dtos = posts.Select(MapToDto).ToList();
        foreach (var dto in dtos)
        {
            dto.Likes = await likeManager.GetPostLikesAsync(dto.Id, dto.Likes);
            dto.Dislikes = await likeManager.GetPostDislikesAsync(dto.Id, dto.Dislikes);
            dto.Watch = await GetPostWatchAsync(dto.Id, dto.Watch);
        }

        return dtos;
    }

    public async Task<List<PostDto>> GetHotAsync(int count = 10)
    {
        var memCacheKey = $"hot_posts_mem_{count}";

        // 1. 本地 LRU 缓存检查 (极高并发拦截)
        if (memoryCache.TryGetValue<List<PostDto>>(memCacheKey, out var memCachedList) && memCachedList != null)
        {
            logger.LogDebug("命中本地 LRU 热榜缓存 (count={Count})", count);
            return memCachedList;
        }

        var distCacheKey = $"hot_posts_dist_{count}";
        List<PostDto>? dtos = null;

        // 2. Redis 分布式缓存检查
        try
        {
            var cachedData = await distributedCache.GetStringAsync(distCacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                dtos = JsonSerializer.Deserialize<List<PostDto>>(cachedData);
                if (dtos != null)
                {
                    logger.LogInformation("命中 Redis 分布式热榜缓存 (count={Count})", count);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "读取 Redis 分布式热榜缓存失败");
        }

        // 3. 缓存穿透，读取数据库
        if (dtos == null)
        {
            var posts = await postRepo.GetHotAsync(count);
            dtos = posts.Select(MapToDto).ToList();

            foreach (var dto in dtos)
            {
                dto.Likes = await likeManager.GetPostLikesAsync(dto.Id, dto.Likes);
                dto.Dislikes = await likeManager.GetPostDislikesAsync(dto.Id, dto.Dislikes);
                dto.Watch = await GetPostWatchAsync(dto.Id, dto.Watch);
            }

            try
            {
                var serializedData = JsonSerializer.Serialize(dtos);
                await distributedCache.SetStringAsync(distCacheKey, serializedData, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = DistHotCacheTtl
                });
                logger.LogInformation("已将热榜数据写入 Redis 缓存 (count={Count})", count);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "写入 Redis 热榜缓存失败");
            }
        }

        // 更新本地 LRU 缓存 (设置 Size 触发驱逐策略)
        memoryCache.Set(memCacheKey, dtos, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = LocalHotCacheTtl,
            Priority = CacheItemPriority.High,
            Size = 1 // 每个热榜列表作为1个单位
        });

        return dtos;
    }

    public async Task<PostDto?> GetByIdAsync(string id)
    {
        var memCacheKey = $"post_mem_{id}";

        // 1. 本地 LRU 缓存检查
        if (memoryCache.TryGetValue<PostDto>(memCacheKey, out var memCachedDto) && memCachedDto != null)
        {
            // 对于单个帖子详情，实时从 Redis 拉取最新的浏览量以保证数据展示的时效性
            memCachedDto.Watch = await GetPostWatchAsync(id, memCachedDto.Watch);
            return memCachedDto;
        }

        var distCacheKey = $"post_dist_{id}";
        PostDto? dto = null;

        // 2. Redis 分布式缓存检查
        try
        {
            var cachedData = await distributedCache.GetStringAsync(distCacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                dto = JsonSerializer.Deserialize<PostDto>(cachedData);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read post cache from Redis");
        }

        // 3. 缓存穿透，读取数据库
        if (dto == null)
        {
            var post = await postRepo.GetByIdAsync(id);
            if (post == null) return null;
            dto = MapToDto(post);
        }

        // 填充动态数据 (点赞/点踩/浏览量)
        dto.Likes = await likeManager.GetPostLikesAsync(dto.Id, dto.Likes);
        dto.Dislikes = await likeManager.GetPostDislikesAsync(dto.Id, dto.Dislikes);
        dto.Watch = await GetPostWatchAsync(dto.Id, dto.Watch);

        // 更新 Redis 缓存
        try
        {
            var serializedData = JsonSerializer.Serialize(dto);
            await distributedCache.SetStringAsync(distCacheKey, serializedData, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = DistPostCacheTtl
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to write post cache to Redis");
        }

        // 更新本地 LRU 缓存 (设置 Size 触发驱逐策略)
        memoryCache.Set(memCacheKey, dto, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = LocalPostCacheTtl,
            Priority = CacheItemPriority.Normal,
            Size = 1 // 帖子详情作为1个单位
        });

        return dto;
    }

    public async Task IncrementWatchAsync(string id)
    {
        if (_redis == null)
        {
            await postRepo.IncrementWatchAsync(id);
            return;
        }

        try
        {
            var db = _redis.GetDatabase();
            var watchKey = $"post:{id}:watch";

            // 原子自增 Redis 浏览量
            var currentWatch = await db.StringIncrementAsync(watchKey);

            // 浏览量每满 WatchSyncThreshold 次，异步写入一次数据库 (削峰填谷)
            if (currentWatch > 0 && currentWatch % WatchSyncThreshold == 0)
            {
                // Fire and forget 后台异步更新，不阻塞当前请求
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await postRepo.AddWatchCountAsync(id, WatchSyncThreshold);
                        logger.LogDebug("成功异步同步 {Count} 次浏览量到数据库, 帖子: {PostId}", WatchSyncThreshold, id);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "异步同步帖子浏览量到数据库失败: {PostId}", id);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "在 Redis 自增浏览量失败，将直接访问数据库，帖子: {PostId}", id);
            await postRepo.IncrementWatchAsync(id);
        }
    }

    public async Task<PostDto?> CreateAsync(PostCreateDto createDto)
    {
        var user = await userRepo.GetByIdAsync(createDto.UserId);
        if (user == null) return null;

        var post = new PostModel
        {
            Title = createDto.Title,
            Content = createDto.Content,
            UserId = createDto.UserId,
            User = user,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            Likes = 0,
            Dislikes = 0,
            Watch = 0
        };

        await postRepo.CreateAsync(post);
        ClearLocalAndDistCache(post.Id);
        return MapToDto(post);
    }

    public async Task<PostDto?> UpdateAsync(string id, PostUpdateDto updateDto)
    {
        var post = await postRepo.GetByIdAsync(id);
        if (post == null) return null;

        post.Title = updateDto.Title;
        post.Content = updateDto.Content;
        post.UpdatedAt = DateTime.Now;

        await postRepo.UpdateAsync(post);
        ClearLocalAndDistCache(id);
        return MapToDto(post);
    }

    public async Task DeleteAsync(string id)
    {
        await postRepo.DeleteAsync(id);
        ClearLocalAndDistCache(id);
    }

    public async Task SetLikesAsync(string userId, string postId)
    {
        var post = await postRepo.GetByIdAsync(postId);
        if (post != null)
        {
            await likeManager.SetPostLikeAsync(postId, userId);
            ClearLocalAndDistCache(postId); // 互动后也清除详细页缓存，以尽早展示新状态
        }
    }

    public async Task SetDislikesAsync(string userId, string postId)
    {
        var post = await postRepo.GetByIdAsync(postId);
        if (post != null)
        {
            await likeManager.SetPostDislikeAsync(postId, userId);
            ClearLocalAndDistCache(postId);
        }
    }

    public async Task<List<CommentDto>> GetCommentsAsync(string id, string type)
    {
        var comments = await postRepo.GetCommentsAsync(id, type);
        return comments == null ? [] : comments.Select(CommentService.MapToDto).ToList();
    }

    /// <summary>
    /// 从 Redis 获取帖子浏览量
    /// </summary>
    private async Task<int> GetPostWatchAsync(string postId, int fallbackCount)
    {
        if (_redis == null) return fallbackCount;
        try
        {
            var db = _redis.GetDatabase();
            var watchKey = $"post:{postId}:watch";
            var val = await db.StringGetAsync(watchKey);
            if (val.HasValue && int.TryParse(val.ToString(), out var count))
            {
                // 取 Redis 与 DB 数据中的最大值，以保证显示的数据不会发生回退
                return Math.Max(count, fallbackCount);
            }
            else if (fallbackCount > 0)
            {
                // 如果 Redis 里没有，就将数据库里最新的值初始化到 Redis
                await db.StringSetAsync(watchKey, fallbackCount.ToString());
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "从 Redis 获取浏览量失败，帖子: {PostId}", postId);
        }

        return fallbackCount;
    }

    /// <summary>
    /// 缓存失效处理
    /// </summary>
    private void ClearLocalAndDistCache(string postId)
    {
        memoryCache.Remove($"post_mem_{postId}");
        _ = distributedCache.RemoveAsync($"post_dist_{postId}");
    }

    public static PostDto MapToDto(PostModel post)
    {
        return new PostDto
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            UserId = post.UserId,
            Likes = post.Likes,
            Dislikes = post.Dislikes,
            Watch = post.Watch
        };
    }
}