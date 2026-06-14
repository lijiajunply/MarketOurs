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
    Task<PostDto?> GetByIdAsync(string id, string? requesterUserId = null);

    /// <summary>
    /// 根据ID获取帖子详情，包含待审核帖子
    /// </summary>
    /// <param name="id">帖子ID</param>
    /// <returns>帖子DTO，不存在则返回null</returns>
    Task<PostDto?> GetByIdIncludingPendingAsync(string id, string? requesterUserId = null);

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
    /// 设置用户对帖子的点赞状态，返回操作后的最新状态
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="postId">帖子ID</param>
    Task<LikeToggleResult> SetLikesAsync(string userId, string postId);

    /// <summary>
    /// 设置用户对帖子的点踩状态，返回操作后的最新状态
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="postId">帖子ID</param>
    Task<LikeToggleResult> SetDislikesAsync(string userId, string postId);

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

    /// <summary>
    /// 管理员单独更新帖子标签，不触发内容重新审核
    /// </summary>
    Task<PostDto> UpdateTagAsync(string id, string? tagId);
}

public class PostService(
    IPostRepo postRepo,
    ICommentRepo commentRepo,
    IUserRepo userRepo,
    ILikeManager likeManager,
    IDistributedCache distributedCache,
    IMemoryCache memoryCache,
    IEnumerable<IConnectionMultiplexer> redisEnumerable,
    ILogger<PostService> logger,
    UploadKeyService uploadKeyService,
    IStorageService storageService,
    IPostTagService? postTagService = null,
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
        await Task.WhenAll(dtos.Select(dto => FillDynamicData(dto)));

        return PagedResultDto<PostDto>.Success(dtos, totalCount, @params.PageIndex, @params.PageSize);
    }

    /// <inheritdoc/>
    public async Task<List<PostDto>> GetHotAsync(int count = 10)
    {
        var memCacheKey = CacheKeys.HotPostsMem(count);
        if (memoryCache.TryGetValue<List<PostDto>>(memCacheKey, out var memCachedList) && memCachedList != null)
        {
            return await FillListAsync(memCachedList);
        }

        await CacheLock.WaitAsync();
        try
        {
            if (memoryCache.TryGetValue<List<PostDto>>(memCacheKey, out var retryMemCachedList) &&
                retryMemCachedList != null)
            {
                return await FillListAsync(retryMemCachedList);
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

            return await FillListAsync(dtos);
        }
        finally
        {
            CacheLock.Release();
        }
    }

    /// <summary>
    /// 并行填充列表的动态数据。为避免污染缓存中的共享 DTO 对象，
    /// 先克隆每个 DTO 再填充（与 GetByIdAsync 中 ClonePostDto 的做法一致）。
    /// </summary>
    private async Task<List<PostDto>> FillListAsync(List<PostDto> source, string? requesterUserId = null)
    {
        var clones = source.Select(ClonePostDto).ToList();
        await Task.WhenAll(clones.Select(dto => FillDynamicData(dto, requesterUserId)));
        return clones;
    }

    /// <inheritdoc/>
    public async Task<PagedResultDto<PostDto>> GetByUserIdAsync(string userId, PaginationParams @params)
    {
        var totalCount = await postRepo.CountByUserIdAsync(userId);
        var posts = await postRepo.GetByUserIdAsync(userId, @params.PageIndex, @params.PageSize);
        var dtos = posts.Select(MapToDto).ToList();

        await Task.WhenAll(dtos.Select(dto => FillDynamicData(dto)));

        return PagedResultDto<PostDto>.Success(dtos, totalCount, @params.PageIndex, @params.PageSize);
    }

    private readonly ConcurrentDictionary<string, Task<PostDto?>> _getByIdTasks = new();

    /// <inheritdoc/>
    public async Task<PostDto?> GetByIdAsync(string id, string? requesterUserId = null)
    {
        var memCacheKey = CacheKeys.PostMem(id);
        if (memoryCache.TryGetValue<PostDto>(memCacheKey, out var memCachedDto) && memCachedDto != null)
        {
            return await FillDynamicData(ClonePostDto(memCachedDto), requesterUserId);
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
        return await FillDynamicData(ClonePostDto(resultDto), requesterUserId);
    }

    public async Task<PostDto?> GetByIdIncludingPendingAsync(string id, string? requesterUserId = null)
    {
        var post = await postRepo.GetByIdAsync(id);
        return post == null ? null : await FillDynamicData(MapToDto(post), requesterUserId);
    }

    private async Task<PostDto> FillDynamicData(PostDto dto, string? requesterUserId = null)
    {
        // 并发发起所有独立的 Redis 读取，借助 StackExchange.Redis 的自动 pipelining
        // 将原本逐个 await 的串行往返合并为少量并发波次，显著降低延迟
        var likesTask = likeManager.GetPostLikesAsync(dto.Id, dto.Likes);
        var dislikesTask = likeManager.GetPostDislikesAsync(dto.Id, dto.Dislikes);
        var watchTask = GetPostWatchAsync(dto.Id, dto.Watch);

        Task<bool>? likedTask = null;
        Task<bool>? dislikedTask = null;
        if (!string.IsNullOrWhiteSpace(requesterUserId))
        {
            likedTask = likeManager.IsPostLikedAsync(dto.Id, requesterUserId);
            dislikedTask = likeManager.IsPostDislikedAsync(dto.Id, requesterUserId);
        }

        dto.Likes = await likesTask;
        dto.Dislikes = await dislikesTask;
        dto.Watch = await watchTask;
        dto.IsLiked = likedTask != null && await likedTask;
        dto.IsDisliked = dislikedTask != null && await dislikedTask;
        return dto;
    }

    private static PostDto ClonePostDto(PostDto dto)
    {
        return new PostDto
        {
            Id = dto.Id,
            Title = dto.Title,
            Content = dto.Content,
            Images = dto.Images.ToList(),
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            UserId = dto.UserId,
            Author = dto.Author,
            TagId = dto.TagId,
            Tag = dto.Tag,
            Likes = dto.Likes,
            Dislikes = dto.Dislikes,
            IsLiked = dto.IsLiked,
            IsDisliked = dto.IsDisliked,
            Watch = dto.Watch,
            IsReview = dto.IsReview
        };
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

        var images = NormalizeImages(createDto.Images);
        var tag = postTagService == null
            ? null
            : await postTagService.GetValidTagForPostAsync(createDto.TagId);
        var post = new PostModel
        {
            Title = createDto.Title,
            Content = createDto.Content,
            Images = images,
            UserId = createDto.UserId,
            TagId = tag?.Id,
            Tag = tag,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Likes = 0,
            Dislikes = 0,
            Watch = 0,
            IsReview = false
        };

        try
        {
            await postRepo.CreateAsync(post);
            InvalidateGlobalCaches();

            // 确认上传密钥：移除 Redis 追踪，文件正式归属于帖子
            if (!string.IsNullOrWhiteSpace(createDto.UploadKey))
            {
                var trackedImages = await uploadKeyService.GetAndRemoveFilesAsync(createDto.UploadKey);
                if (MergeTrackedImages(post, trackedImages))
                {
                    await postRepo.UpdateAsync(post);
                    InvalidateCache(post.Id);
                }
            }

            if (reviewQueue != null)
            {
                await reviewQueue.EnqueueAsync(new ReviewMessage(post.Id, ReviewType.Post));
            }
        }
        catch
        {
            // 创建失败时清理上传密钥关联的文件
            if (!string.IsNullOrWhiteSpace(createDto.UploadKey))
            {
                await uploadKeyService.DeleteFilesByKeyAsync(createDto.UploadKey, storageService);
            }

            throw;
        }

        return MapToDto(post);
    }

    public async Task<PostDto> UpdateAsync(string id, PostUpdateDto updateDto, bool isAdmin = false)
    {
        var post = await postRepo.GetByIdAsync(id);
        if (post == null) throw new ResourceAccessException(ErrorCode.PostNotFound, "帖子不存在");

        post.Title = updateDto.Title;
        post.Content = updateDto.Content;
        post.Images = NormalizeImages(updateDto.Images);
        var tag = postTagService == null
            ? null
            : await postTagService.GetValidTagForPostAsync(updateDto.TagId, isAdmin);
        post.TagId = tag?.Id;
        post.Tag = tag;
        post.UpdatedAt = DateTime.UtcNow;
        post.IsReview = isAdmin;

        try
        {
            await postRepo.UpdateAsync(post);
            InvalidateCache(id);

            // 确认上传密钥
            if (!string.IsNullOrWhiteSpace(updateDto.UploadKey))
            {
                var trackedImages = await uploadKeyService.GetAndRemoveFilesAsync(updateDto.UploadKey);
                if (MergeTrackedImages(post, trackedImages))
                {
                    await postRepo.UpdateAsync(post);
                    InvalidateCache(id);
                }
            }

            if (reviewQueue != null && !isAdmin)
            {
                await reviewQueue.EnqueueAsync(new ReviewMessage(post.Id, ReviewType.Post));
            }
        }
        catch
        {
            // 更新失败时清理上传密钥关联的文件
            if (!string.IsNullOrWhiteSpace(updateDto.UploadKey))
            {
                await uploadKeyService.DeleteFilesByKeyAsync(updateDto.UploadKey, storageService);
            }

            throw;
        }

        return MapToDto(post);
    }

    private static List<string> NormalizeImages(IEnumerable<string>? images)
    {
        return images?
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.Ordinal)
            .ToList() ?? [];
    }

    private static bool MergeTrackedImages(PostModel post, IEnumerable<string>? trackedImages)
    {
        var merged = post.Images
            .Concat(trackedImages ?? [])
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (merged.SequenceEqual(post.Images)) return false;

        post.Images = merged;
        post.UpdatedAt = DateTime.UtcNow;
        return true;
    }

    public async Task DeleteAsync(string id)
    {
        var post = await postRepo.GetByIdAsync(id);
        if (post == null) throw new ResourceAccessException(ErrorCode.PostNotFound, "帖子不存在");
        await postRepo.DeleteAsync(id);
        InvalidateCache(id);
    }

    /// <inheritdoc/>
    public async Task<LikeToggleResult> SetLikesAsync(string userId, string postId)
    {
        var post = await postRepo.GetByIdAsync(postId);
        if (post == null) throw new ResourceAccessException(ErrorCode.PostNotFound, "帖子不存在");

        return await likeManager.SetPostLikeAsync(postId, userId);
    }

    /// <inheritdoc/>
    public async Task<LikeToggleResult> SetDislikesAsync(string userId, string postId)
    {
        var post = await postRepo.GetByIdAsync(postId);
        if (post == null) throw new ResourceAccessException(ErrorCode.PostNotFound, "帖子不存在");

        return await likeManager.SetPostDislikeAsync(postId, userId);
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

        // 登录用户:一次性批量查出该用户在本帖下点赞/点踩的评论集合,
        // 替代原先逐条评论各 2 次的 IsCommentLiked/Disliked 查询(N+1 → 2 次查询)。
        HashSet<string>? likedSet = null;
        HashSet<string>? dislikedSet = null;
        if (!string.IsNullOrWhiteSpace(requesterUserId))
        {
            (likedSet, dislikedSet) = await commentRepo.GetUserCommentReactionsAsync(id, requesterUserId);
        }

        // 填充点赞/点踩计数(O(1) Redis SCARD,并行执行);点赞状态用上面的批量集合判断。
        await Task.WhenAll(allDtos.Select(async dto =>
        {
            dto.Likes = await likeManager.GetCommentLikesAsync(dto.Id, dto.Likes);
            dto.Dislikes = await likeManager.GetCommentDislikesAsync(dto.Id, dto.Dislikes);
            if (likedSet != null)
            {
                dto.IsLiked = likedSet.Contains(dto.Id);
                dto.IsDisliked = dislikedSet!.Contains(dto.Id);
            }
        }));

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
        var keyword = @params.Keyword?.Trim();
        if (string.IsNullOrWhiteSpace(keyword))
            return PagedResultDto<PostDto>.Success([], 0, @params.PageIndex, @params.PageSize);

        var totalCount = await postRepo.SearchCountAsync(keyword);
        var results = await postRepo.SearchAsync(keyword, @params.PageIndex, @params.PageSize);
        var dtos = results.Select(MapToDto).ToList();

        await Task.WhenAll(dtos.Select(dto => FillDynamicData(dto)));

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

    public async Task<PostDto> UpdateTagAsync(string id, string? tagId)
    {
        var post = await postRepo.GetByIdAsync(id);
        if (post == null) throw new ResourceAccessException(ErrorCode.PostNotFound, "帖子不存在");

        var tag = postTagService == null
            ? null
            : await postTagService.GetValidTagForPostAsync(tagId, allowInactive: true);

        await postRepo.SetTagAsync(id, tag?.Id);
        post.TagId = tag?.Id;
        post.Tag = tag;
        post.UpdatedAt = DateTime.UtcNow;
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
            TagId = post.TagId,
            Tag = post.Tag == null ? null : PostTagService.MapToDto(post.Tag),
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
