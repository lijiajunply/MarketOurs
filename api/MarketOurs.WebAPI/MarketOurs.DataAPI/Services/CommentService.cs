using MarketOurs.DataAPI.Configs;
using MarketOurs.Data.DataModels;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Repos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MarketOurs.DataAPI.Services;

/// <summary>
/// 评论服务接口，处理评论的创建、查询、更新、删除及点赞等业务，支持树形结构显示
/// </summary>
public interface ICommentService
{
    /// <summary>
    /// 获取所有评论 (分页)
    /// </summary>
    Task<PagedResultDto<CommentDto>> GetAllAsync(PaginationParams @params);

    /// <summary>
    /// 搜索评论 (基于关键词)
    /// </summary>
    Task<PagedResultDto<CommentDto>> SearchAsync(PaginationParams @params);

    /// <summary>
    /// 根据 ID 获取评论详情
    /// </summary>
    Task<CommentDto?> GetByIdAsync(string id);

    /// <summary>
    /// 创建新评论 (支持回复)
    /// </summary>
    Task<CommentDto?> CreateAsync(CommentCreateDto createDto);

    /// <summary>
    /// 更新评论内容
    /// </summary>
    Task<CommentDto?> UpdateAsync(string id, CommentUpdateDto updateDto);

    /// <summary>
    /// 删除评论
    /// </summary>
    Task DeleteAsync(string id);

    /// <summary>
    /// 设置用户对评论的点赞状态
    /// </summary>
    Task SetLikesAsync(string userId, string commentId);

    /// <summary>
    /// 设置用户对评论的点踩状态
    /// </summary>
    Task SetDislikesAsync(string userId, string commentId);
}

public class CommentService(
    ICommentRepo commentRepo, 
    IUserRepo userRepo, 
    IPostRepo postRepo,
    ILikeManager likeManager,
    IMemoryCache memoryCache,
    IDistributedCache distributedCache,
    Background.NotificationMessageQueue notificationQueue,
    ILogger<CommentService> logger) : ICommentService
{
    private static readonly TimeSpan LocalCacheTtl = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan DistCacheTtl = TimeSpan.FromMinutes(10);

    /// <inheritdoc/>
    public async Task<PagedResultDto<CommentDto>> GetAllAsync(PaginationParams @params)
    {
        var totalCount = await commentRepo.CountAsync();
        var comments = await commentRepo.GetAllAsync(@params.PageIndex, @params.PageSize);
        var dtos = comments.Select(MapToDto).ToList();
        foreach (var dto in dtos)
        {
            dto.Likes = await likeManager.GetCommentLikesAsync(dto.Id, dto.Likes);
            dto.Dislikes = await likeManager.GetCommentDislikesAsync(dto.Id, dto.Dislikes);
        }
        return PagedResultDto<CommentDto>.Success(dtos, totalCount, @params.PageIndex, @params.PageSize);
    }

    public async Task<PagedResultDto<CommentDto>> SearchAsync(PaginationParams @params)
    {
        if (string.IsNullOrWhiteSpace(@params.Keyword))
            return PagedResultDto<CommentDto>.Success([], 0, @params.PageIndex, @params.PageSize);

        var totalCount = await commentRepo.SearchCountAsync(@params.Keyword);
        var comments = await commentRepo.SearchAsync(@params.Keyword, @params.PageIndex, @params.PageSize);
        var dtos = comments.Select(MapToDto).ToList();
        foreach (var dto in dtos)
        {
            await FillDynamicData(dto);
        }
        return PagedResultDto<CommentDto>.Success(dtos, totalCount, @params.PageIndex, @params.PageSize);
    }

    public async Task<CommentDto?> GetByIdAsync(string id)
    {
        var memKey = CacheKeys.CommentMem(id);
        if (memoryCache.TryGetValue<CommentDto>(memKey, out var memDto) && memDto != null)
        {
            return await FillDynamicData(memDto);
        }

        var distKey = CacheKeys.CommentDist(id);
        CommentDto? dto = null;

        try
        {
            var cachedData = await distributedCache.GetStringAsync(distKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                dto = JsonSerializer.Deserialize<CommentDto>(cachedData);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read comment cache from Redis");
        }

        if (dto == null)
        {
            var comment = await commentRepo.GetByIdAsync(id);
            if (comment == null) return null;
            dto = MapToDto(comment);

            try
            {
                await distributedCache.SetStringAsync(distKey, JsonSerializer.Serialize(dto), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = DistCacheTtl
                });
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to write comment cache to Redis");
            }
        }

        memoryCache.Set(memKey, dto, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = LocalCacheTtl,
            Size = 1
        });

        return await FillDynamicData(dto);
    }

    private async Task<CommentDto> FillDynamicData(CommentDto dto)
    {
        dto.Likes = await likeManager.GetCommentLikesAsync(dto.Id, dto.Likes);
        dto.Dislikes = await likeManager.GetCommentDislikesAsync(dto.Id, dto.Dislikes);
        return dto;
    }

    public async Task<CommentDto?> CreateAsync(CommentCreateDto createDto)
    {
        var user = await userRepo.GetByIdAsync(createDto.UserId);
        if (user == null) return null;

        var comment = new CommentModel
        {
            Content = createDto.Content,
            Images = createDto.Images,
            UserId = createDto.UserId,
            PostId = createDto.PostId,
            ParentCommentId = createDto.ParentCommentId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Likes = 0,
            Dislikes = 0
        };

        await commentRepo.CreateAsync(comment);
        // 清除该贴子的评论列表缓存（如果有）
        InvalidateCommentListCache(comment.PostId);

        // 推送通知逻辑
        try
        {
            var commenter = user;
            if (string.IsNullOrEmpty(comment.ParentCommentId))
            {
                // 贴子主评论，通知贴子作者
                var post = await postRepo.GetByIdAsync(comment.PostId);
                if (post != null && post.UserId != comment.UserId)
                {
                    notificationQueue.Enqueue(new Background.NotificationMessage
                    {
                        UserId = post.UserId,
                        Title = "你的贴子收到了新评论",
                        Content = $"{commenter.Name} 评论了你的贴子: {comment.Content.Substring(0, Math.Min(comment.Content.Length, 20))}...",
                        Type = NotificationType.PostReply,
                        TargetId = comment.PostId
                    });
                }
            }
            else
            {
                // 回复评论，通知被回复的人
                var parentComment = await commentRepo.GetByIdAsync(comment.ParentCommentId);
                if (parentComment != null && parentComment.UserId != comment.UserId)
                {
                    notificationQueue.Enqueue(new Background.NotificationMessage
                    {
                        UserId = parentComment.UserId,
                        Title = "你的评论收到了回复",
                        Content = $"{commenter.Name} 回复了你: {comment.Content[..Math.Min(comment.Content.Length, 20)]}...",
                        Type = NotificationType.CommentReply,
                        TargetId = comment.PostId // 指向贴子
                    });
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to enqueue notification for comment creation");
        }

        return MapToDto(comment);
    }

    public async Task<CommentDto?> UpdateAsync(string id, CommentUpdateDto updateDto)
    {
        var comment = await commentRepo.GetByIdAsync(id);
        if (comment == null) return null;

        comment.Content = updateDto.Content;
        comment.Images = updateDto.Images;
        comment.UpdatedAt = DateTime.Now;

        await commentRepo.UpdateAsync(comment);
        InvalidateCache(id, comment.PostId);
        return MapToDto(comment);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string id)
    {
        var comment = await commentRepo.GetByIdAsync(id);
        if (comment != null)
        {
            await commentRepo.DeleteAsync(id);
            InvalidateCache(id, comment.PostId);
        }
    }

    public async Task SetLikesAsync(string userId, string commentId)
    {
        var comment = await commentRepo.GetByIdAsync(commentId);
        if (comment != null)
        {
            await likeManager.SetCommentLikeAsync(commentId, userId);
            // 互动操作不需要清除 DTO 缓存，因为 FillDynamicData 会实时获取最新计数
        }
    }

    public async Task SetDislikesAsync(string userId, string commentId)
    {
        var comment = await commentRepo.GetByIdAsync(commentId);
        if (comment != null)
        {
            await likeManager.SetCommentDislikeAsync(commentId, userId);
        }
    }

    private void InvalidateCache(string id, string postId)
    {
        memoryCache.Remove(CacheKeys.CommentMem(id));
        _ = distributedCache.RemoveAsync(CacheKeys.CommentDist(id));
        InvalidateCommentListCache(postId);
    }

    private void InvalidateCommentListCache(string postId)
    {
        // 如果有针对贴子的评论列表缓存，在此清除
        memoryCache.Remove(CacheKeys.PostComments(postId));
    }

    public static CommentDto MapToDto(CommentModel comment)
    {
        return new CommentDto
        {
            Id = comment.Id,
            Content = comment.Content,
            Images = comment.Images,
            Likes = comment.Likes,
            Dislikes = comment.Dislikes,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt,
            UserId = comment.UserId,
            Author = comment.User != null ? new UserSimpleDto
            {
                Id = comment.User.Id,
                Name = comment.User.Name,
                Avatar = comment.User.Avatar
            } : null,
            PostId = comment.PostId,
            ParentCommentId = comment.ParentCommentId
        };
    }
}
