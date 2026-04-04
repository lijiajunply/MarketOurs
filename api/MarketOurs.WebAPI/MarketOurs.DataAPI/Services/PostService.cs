using MarketOurs.DataAPI.Configs;
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
    Task<List<PostDto>> SearchAsync(string keyword);
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
    private const int WatchSyncThreshold = 10;

    public async Task<List<PostDto>> GetAllAsync()
    {
        var posts = await postRepo.GetAllAsync();
        var dtos = posts.Select(MapToDto).ToList();
        foreach (var dto in dtos)
        {
            await FillDynamicData(dto);
        }
        return dtos;
    }

    public async Task<List<PostDto>> GetHotAsync(int count = 10)
    {
        var memCacheKey = CacheKeys.HotPostsMem(count);
        if (memoryCache.TryGetValue<List<PostDto>>(memCacheKey, out var memCachedList) && memCachedList != null)
        {
            foreach (var dto in memCachedList) await FillDynamicData(dto);
            return memCachedList;
        }

        var distCacheKey = CacheKeys.HotPostsDist(count);
        List<PostDto>? dtos = null;

        try
        {
            var cachedData = await distributedCache.GetStringAsync(distCacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                dtos = JsonSerializer.Deserialize<List<PostDto>>(cachedData);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read hot posts from Redis");
        }

        if (dtos == null)
        {
            var posts = await postRepo.GetHotAsync(count);
            dtos = posts.Select(MapToDto).ToList();
            try
            {
                await distributedCache.SetStringAsync(distCacheKey, JsonSerializer.Serialize(dtos), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = DistHotCacheTtl
                });
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to write hot posts to Redis");
            }
        }

        memoryCache.Set(memCacheKey, dtos, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = LocalHotCacheTtl,
            Size = 1
        });

        foreach (var dto in dtos) await FillDynamicData(dto);
        return dtos;
    }

    public async Task<PostDto?> GetByIdAsync(string id)
    {
        var memCacheKey = CacheKeys.PostMem(id);
        if (memoryCache.TryGetValue<PostDto>(memCacheKey, out var memCachedDto) && memCachedDto != null)
        {
            return await FillDynamicData(memCachedDto);
        }

        var distCacheKey = CacheKeys.PostDist(id);
        PostDto? dto = null;

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

        if (dto == null)
        {
            var post = await postRepo.GetByIdAsync(id);
            if (post == null) return null;
            dto = MapToDto(post);

            try
            {
                await distributedCache.SetStringAsync(distCacheKey, JsonSerializer.Serialize(dto), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = DistPostCacheTtl
                });
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to write post cache to Redis");
            }
        }

        memoryCache.Set(memCacheKey, dto, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = LocalPostCacheTtl,
            Size = 1
        });

        return await FillDynamicData(dto);
    }

    private async Task<PostDto> FillDynamicData(PostDto dto)
    {
        dto.Likes = await likeManager.GetPostLikesAsync(dto.Id, dto.Likes);
        dto.Dislikes = await likeManager.GetPostDislikesAsync(dto.Id, dto.Dislikes);
        dto.Watch = await GetPostWatchAsync(dto.Id, dto.Watch);
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
            var watchKey = CacheKeys.PostWatch(id);
            var currentWatch = await db.StringIncrementAsync(watchKey);

            if (currentWatch > 0 && currentWatch % WatchSyncThreshold == 0)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await postRepo.AddWatchCountAsync(id, WatchSyncThreshold);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to async sync watch count: {PostId}", id);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to increment watch in Redis: {PostId}", id);
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
            Images = createDto.Images,
            UserId = createDto.UserId,
            User = user,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            Likes = 0,
            Dislikes = 0,
            Watch = 0
        };

        await postRepo.CreateAsync(post);
        InvalidateGlobalCaches();
        return MapToDto(post);
    }

    public async Task<PostDto?> UpdateAsync(string id, PostUpdateDto updateDto)
    {
        var post = await postRepo.GetByIdAsync(id);
        if (post == null) return null;

        post.Title = updateDto.Title;
        post.Content = updateDto.Content;
        post.Images = updateDto.Images;
        post.UpdatedAt = DateTime.Now;

        await postRepo.UpdateAsync(post);
        InvalidateCache(id);
        return MapToDto(post);
    }

    public async Task DeleteAsync(string id)
    {
        await postRepo.DeleteAsync(id);
        InvalidateCache(id);
    }

    public async Task SetLikesAsync(string userId, string postId)
    {
        var post = await postRepo.GetByIdAsync(postId);
        if (post != null)
        {
            await likeManager.SetPostLikeAsync(postId, userId);
            // Dynamic data fills automatically, no need to invalidate DTO cache
        }
    }

    public async Task SetDislikesAsync(string userId, string postId)
    {
        var post = await postRepo.GetByIdAsync(postId);
        if (post != null)
        {
            await likeManager.SetPostDislikeAsync(postId, userId);
        }
    }

    public async Task<List<CommentDto>> GetCommentsAsync(string id, string type)
    {
        // 尝试从本地缓存读取评论列表
        var cacheKey = CacheKeys.PostComments(id);
        if (memoryCache.TryGetValue<List<CommentDto>>(cacheKey, out var cachedComments) && cachedComments != null)
        {
            return cachedComments;
        }

        var comments = await postRepo.GetCommentsAsync(id, type);
        var dtos = comments == null ? [] : comments.Select(CommentService.MapToDto).ToList();
        
        memoryCache.Set(cacheKey, dtos, TimeSpan.FromMinutes(2));
        return dtos;
    }

    public async Task<List<PostDto>> SearchAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return [];
        
        var posts = await postRepo.SearchAsync(keyword);
        var dtos = posts.Select(MapToDto).ToList();

        foreach (var dto in dtos)
        {
            await FillDynamicData(dto);
        }

        return dtos;
    }

    private async Task<int> GetPostWatchAsync(string postId, int fallbackCount)
    {
        if (_redis == null) return fallbackCount;
        try
        {
            var db = _redis.GetDatabase();
            var watchKey = CacheKeys.PostWatch(postId);
            var val = await db.StringGetAsync(watchKey);
            if (val.HasValue && int.TryParse(val.ToString(), out var count))
            {
                return Math.Max(count, fallbackCount);
            }
            else if (fallbackCount > 0)
            {
                await db.StringSetAsync(watchKey, fallbackCount.ToString());
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get watch count from Redis: {PostId}", postId);
        }

        return fallbackCount;
    }

    private void InvalidateCache(string postId)
    {
        memoryCache.Remove(CacheKeys.PostMem(postId));
        _ = distributedCache.RemoveAsync(CacheKeys.PostDist(postId));
        InvalidateGlobalCaches();
    }

    private void InvalidateGlobalCaches()
    {
        // 清除所有热门列表缓存，因为贴子变更会影响排名
        // 注意：实际生产中可以使用通配符或 CacheTag，这里简单起见清除常见 count 值的缓存
        for (int i = 5; i <= 20; i += 5)
        {
            memoryCache.Remove(CacheKeys.HotPostsMem(i));
            _ = distributedCache.RemoveAsync(CacheKeys.HotPostsDist(i));
        }
    }

    public static PostDto MapToDto(PostModel post)
    {
        return new PostDto
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            Images = post.Images,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            UserId = post.UserId,
            Likes = post.Likes,
            Dislikes = post.Dislikes,
            Watch = post.Watch
        };
    }
}
