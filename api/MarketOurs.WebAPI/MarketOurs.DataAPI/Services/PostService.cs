using MarketOurs.DataAPI.Configs;
using System.Collections.Concurrent;
using System.Text.Json;
using MarketOurs.Data.DataModels;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Exceptions;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services.Background;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MarketOurs.DataAPI.Services;

/// <summary>
/// 帖子服务接口，处理帖子的增删改查、缓存逻辑、点击量统计及点赞等业务
/// </summary>
public interface IPostService
{
    /// <summary>
    /// 分页获取所有帖子
    /// </summary>
    /// <param name="params">分页参数</param>
    /// <returns>分页结果</returns>
    Task<PagedResultDto<PostDto>> GetAllAsync(PaginationParams @params);

    /// <summary>
    /// 获取热门帖子列表
    /// </summary>
    /// <param name="count">获取数量</param>
    /// <returns>帖子列表</returns>
    Task<List<PostDto>> GetHotAsync(int count = 10);

    /// <summary>
    /// 分页获取指定用户发布的帖子
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="params">分页参数</param>
    /// <returns>分页结果</returns>
    Task<PagedResultDto<PostDto>> GetByUserIdAsync(string userId, PaginationParams @params);

    /// <summary>
    /// 根据ID获取帖子详情
    /// </summary>
    /// <param name="id">帖子ID</param>
    /// <returns>帖子DTO，不存在则返回null</returns>
    Task<PostDto?> GetByIdAsync(string id);

    /// <summary>
    /// 根据ID获取帖子详情，包含待审核帖子
    /// </summary>
    /// <param name="id">帖子ID</param>
    /// <returns>帖子DTO，不存在则返回null</returns>
    Task<PostDto?> GetByIdIncludingPendingAsync(string id);

    /// <summary>
    /// 创建新帖子
    /// </summary>
    /// <param name="createDto">创建参数</param>
    /// <returns>创建成功的帖子DTO</returns>
    Task<PostDto> CreateAsync(PostCreateDto createDto);

    /// <summary>
    /// 更新帖子内容
    /// </summary>
    /// <param name="id">帖子ID</param>
    /// <param name="updateDto">更新参数</param>
    /// <param name="isAdmin">是否为管理员</param>
    /// <returns>更新后的帖子DTO</returns>
    Task<PostDto> UpdateAsync(string id, PostUpdateDto updateDto, bool isAdmin = false);

    /// <summary>
    /// 删除帖子
    /// </summary>
    /// <param name="id">帖子ID</param>
    Task DeleteAsync(string id);

    /// <summary>
    /// 增加帖子浏览量 (支持 Redis 缓存同步)
    /// </summary>
    /// <param name="id">帖子ID</param>
    Task IncrementWatchAsync(string id);

    /// <summary>
    /// 设置用户对帖子的点赞状态
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="postId">帖子ID</param>
    Task SetLikesAsync(string userId, string postId);

    /// <summary>
    /// 设置用户对帖子的点踩状态
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="postId">帖子ID</param>
    Task SetDislikesAsync(string userId, string postId);

    /// <summary>
    /// 获取帖子的评论列表 (构建树形结构)
    /// </summary>
    /// <param name="id">帖子ID</param>
    /// <param name="type">排序类型 (Hot/New)</param>
    /// <returns>评论树列表</returns>
    Task<List<CommentDto>> GetCommentsAsync(string id, string type, string? requesterUserId = null, bool isAdmin = false);

    /// <summary>
    /// 全文搜索帖子 (基于关键词)
    /// </summary>
    /// <param name="params">包含关键词的分页参数</param>
    /// <returns>搜索结果分页对象</returns>
    Task<PagedResultDto<PostDto>> SearchAsync(PaginationParams @params);

    /// <summary>
    /// 更新帖子审核状态
    /// </summary>
    /// <param name="id">帖子 ID</param>
    /// <param name="isReview">是否审核通过</param>
    /// <returns>操作后的帖子DTO</returns>
    Task<PostDto> UpdateReviewAsync(string id, bool isReview);
}

public class PostService(
    IPostRepo postRepo,
    IUserRepo userRepo,
    ILikeManager likeManager,
    IDistributedCache distributedCache,
    IMemoryCache memoryCache,
    IEnumerable<IConnectionMultiplexer> redisEnumerable,
    ILogger<PostService> logger,
    ReviewMessageQueue? reviewQueue = null) : IPostService
{
    private readonly IConnectionMultiplexer? _redis = redisEnumerable.FirstOrDefault();
    private static readonly SemaphoreSlim CacheLock = new(1, 1);

    // 缓存过期时间配置
    private static readonly TimeSpan LocalHotCacheTtl = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan LocalPostCacheTtl = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan DistHotCacheTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DistPostCacheTtl = TimeSpan.FromMinutes(10);
    private const int WatchSyncThreshold = 10;

    /// <inheritdoc/>
    public async Task<PagedResultDto<PostDto>> GetAllAsync(PaginationParams @params)
    {
        var totalCount = await postRepo.CountAsync();
        var posts = await postRepo.GetAllAsync(@params.PageIndex, @params.PageSize);
        var dtos = posts.Select(MapToDto).ToList();
        foreach (var dto in dtos)
        {
            await FillDynamicData(dto);
        }

        return PagedResultDto<PostDto>.Success(dtos, totalCount, @params.PageIndex, @params.PageSize);
    }

    /// <inheritdoc/>
    public async Task<List<PostDto>> GetHotAsync(int count = 10)
    {
        var memCacheKey = CacheKeys.HotPostsMem(count);
        if (memoryCache.TryGetValue<List<PostDto>>(memCacheKey, out var memCachedList) && memCachedList != null)
        {
            foreach (var dto in memCachedList) await FillDynamicData(dto);
            return memCachedList;
        }

        await CacheLock.WaitAsync();
        try
        {
            if (memoryCache.TryGetValue<List<PostDto>>(memCacheKey, out var retryMemCachedList) &&
                retryMemCachedList != null)
            {
                foreach (var dto in retryMemCachedList) await FillDynamicData(dto);
                return retryMemCachedList;
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
                    await distributedCache.SetStringAsync(distCacheKey, JsonSerializer.Serialize(dtos),
                        new DistributedCacheEntryOptions
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
        finally
        {
            CacheLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<PagedResultDto<PostDto>> GetByUserIdAsync(string userId, PaginationParams @params)
    {
        var totalCount = await postRepo.CountByUserIdAsync(userId);
        var posts = await postRepo.GetByUserIdAsync(userId, @params.PageIndex, @params.PageSize);
        var dtos = posts.Select(MapToDto).ToList();

        foreach (var dto in dtos)
        {
            await FillDynamicData(dto);
        }

        return PagedResultDto<PostDto>.Success(dtos, totalCount, @params.PageIndex, @params.PageSize);
    }

    private readonly ConcurrentDictionary<string, Task<PostDto?>> _getByIdTasks = new();

    /// <inheritdoc/>
    public async Task<PostDto?> GetByIdAsync(string id)
    {
        var memCacheKey = CacheKeys.PostMem(id);
        if (memoryCache.TryGetValue<PostDto>(memCacheKey, out var memCachedDto) && memCachedDto != null)
        {
            return await FillDynamicData(memCachedDto);
        }

        // Request coalescing to prevent penetration storm
        var task = _getByIdTasks.GetOrAdd(id, async _ =>
        {
            try
            {
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
                    var post = await postRepo.GetReviewedByIdAsync(id);
                    if (post == null) return null;
                    dto = MapToDto(post);

                    try
                    {
                        await distributedCache.SetStringAsync(distCacheKey, JsonSerializer.Serialize(dto),
                            new DistributedCacheEntryOptions
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

                return dto;
            }
            finally
            {
                _getByIdTasks.TryRemove(id, out var _);
            }
        });

        var resultDto = await task;
        if (resultDto == null) return null;

        // Create a new instance if needed, but FillDynamicData modifies it in-place which might be a race condition if multiple awaiters run it concurrently.
        // Wait, FillDynamicData modifies dto.Likes, etc. The original code also modified it in-place.
        return await FillDynamicData(resultDto);
    }

    public async Task<PostDto?> GetByIdIncludingPendingAsync(string id)
    {
        var post = await postRepo.GetByIdAsync(id);
        return post == null ? null : await FillDynamicData(MapToDto(post));
    }

    private async Task<PostDto> FillDynamicData(PostDto dto)
    {
        dto.Likes = await likeManager.GetPostLikesAsync(dto.Id, dto.Likes);
        dto.Dislikes = await likeManager.GetPostDislikesAsync(dto.Id, dto.Dislikes);
        dto.Watch = await GetPostWatchAsync(dto.Id, dto.Watch);
        return dto;
    }

    /// <inheritdoc/>
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

    public async Task<PostDto> CreateAsync(PostCreateDto createDto)
    {
        var user = await userRepo.GetByIdAsync(createDto.UserId);
        if (user == null) throw new ResourceAccessException(ErrorCode.UserNotFound, "用户不存在");

        var post = new PostModel
        {
            Title = createDto.Title,
            Content = createDto.Content,
            Images = createDto.Images,
            UserId = createDto.UserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Likes = 0,
            Dislikes = 0,
            Watch = 0,
            IsReview = false
        };

        await postRepo.CreateAsync(post);
        InvalidateGlobalCaches();
        if (reviewQueue != null)
        {
            await reviewQueue.EnqueueAsync(new ReviewMessage(post.Id, ReviewType.Post));
        }

        return MapToDto(post);
    }

    public async Task<PostDto> UpdateAsync(string id, PostUpdateDto updateDto, bool isAdmin = false)
    {
        var post = await postRepo.GetByIdAsync(id);
        if (post == null) throw new ResourceAccessException(ErrorCode.PostNotFound, "帖子不存在");

        post.Title = updateDto.Title;
        post.Content = updateDto.Content;
        post.Images = updateDto.Images;
        post.UpdatedAt = DateTime.UtcNow;
        post.IsReview = isAdmin;

        await postRepo.UpdateAsync(post);
        InvalidateCache(id);
        if (reviewQueue != null && !isAdmin)
        {
            await reviewQueue.EnqueueAsync(new ReviewMessage(post.Id, ReviewType.Post));
        }

        return MapToDto(post);
    }

    public async Task DeleteAsync(string id)
    {
        var post = await postRepo.GetByIdAsync(id);
        if (post == null) throw new ResourceAccessException(ErrorCode.PostNotFound, "帖子不存在");
        await postRepo.DeleteAsync(id);
        InvalidateCache(id);
    }

    /// <inheritdoc/>
    public async Task SetLikesAsync(string userId, string postId)
    {
        var post = await postRepo.GetByIdAsync(postId);
        if (post == null) throw new ResourceAccessException(ErrorCode.PostNotFound, "帖子不存在");

        await likeManager.SetPostLikeAsync(postId, userId);
        // Dynamic data fills automatically, no need to invalidate DTO cache
    }

    public async Task SetDislikesAsync(string userId, string postId)
    {
        var post = await postRepo.GetByIdAsync(postId);
        if (post == null) throw new ResourceAccessException(ErrorCode.PostNotFound, "帖子不存在");

        await likeManager.SetPostDislikeAsync(postId, userId);
    }

    /// <inheritdoc/>
    public async Task<List<CommentDto>> GetCommentsAsync(string id, string type, string? requesterUserId = null, bool isAdmin = false)
    {
        // 尝试从本地缓存读取评论列表
        var cacheKey = CacheKeys.PostComments(id);
        if (!isAdmin && string.IsNullOrWhiteSpace(requesterUserId)
            && memoryCache.TryGetValue<List<CommentDto>>(cacheKey, out var cachedComments) && cachedComments != null)
        {
            return cachedComments;
        }

        var comments = await postRepo.GetCommentsAsync(id, type);
        if (comments == null || comments.Count == 0) return [];

        var allDtos = comments
            .Select(CommentService.MapToDto)
            .Where(dto => dto.IsReview || isAdmin || (!string.IsNullOrWhiteSpace(requesterUserId) && dto.UserId == requesterUserId))
            .ToList();

        // 填充动态数据（Likes/Dislikes）
        foreach (var dto in allDtos)
        {
            dto.Likes = await likeManager.GetCommentLikesAsync(dto.Id, dto.Likes);
            dto.Dislikes = await likeManager.GetCommentDislikesAsync(dto.Id, dto.Dislikes);
        }

        // 构建树形结构
        var commentDict = allDtos.ToDictionary(c => c.Id);
        var rootComments = new List<CommentDto>();

        foreach (var dto in allDtos)
        {
            if (string.IsNullOrEmpty(dto.ParentCommentId))
            {
                rootComments.Add(dto);
            }
            else if (commentDict.TryGetValue(dto.ParentCommentId, out var parent))
            {
                parent.RepliedComments.Add(dto);
            }
            else
            {
                // 如果找不到父级，视为根评论（健壮性处理）
                rootComments.Add(dto);
            }
        }

        // 定义排序函数
        void SortComments(List<CommentDto> list)
        {
            if (type == "Like")
                list.Sort((a, b) => b.Likes.CompareTo(a.Likes));
            else
                list.Sort((a, b) => b.UpdatedAt.CompareTo(a.UpdatedAt));

            foreach (var comment in list.Where(comment => comment.RepliedComments.Count != 0))
            {
                SortComments(comment.RepliedComments);
            }
        }

        SortComments(rootComments);

        if (!isAdmin && string.IsNullOrWhiteSpace(requesterUserId))
        {
            memoryCache.Set(cacheKey, rootComments, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2),
                Size = 1
            });
        }

        return rootComments;
    }

    public async Task<PagedResultDto<PostDto>> SearchAsync(PaginationParams @params)
    {
        if (string.IsNullOrWhiteSpace(@params.Keyword))
            return PagedResultDto<PostDto>.Success([], 0, @params.PageIndex, @params.PageSize);

        var totalCount = await postRepo.SearchCountAsync(@params.Keyword);
        var results = await postRepo.SearchAsync(@params.Keyword, @params.PageIndex, @params.PageSize);
        var dtos = results.Select(MapToDto).ToList();

        foreach (var dto in dtos)
        {
            await FillDynamicData(dto);
        }

        return PagedResultDto<PostDto>.Success(dtos, totalCount, @params.PageIndex, @params.PageSize);
    }

    public async Task<PostDto> UpdateReviewAsync(string id, bool isReview)
    {
        var post = await postRepo.GetByIdAsync(id);
        if (post == null) throw new ResourceAccessException(ErrorCode.PostNotFound, "帖子不存在");

        post.IsReview = isReview;
        post.UpdatedAt = DateTime.UtcNow;

        await postRepo.UpdateAsync(post);
        InvalidateCache(id);
        return MapToDto(post);
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

            if (fallbackCount > 0)
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
            Author = post.User != null!
                ? new UserSimpleDto
                {
                    Id = post.User.Id,
                    Name = post.User.Name,
                    Avatar = post.User.Avatar
                }
                : null,
            Likes = post.Likes,
            Dislikes = post.Dislikes,
            Watch = post.Watch,
            IsReview = post.IsReview,
        };
    }
}
