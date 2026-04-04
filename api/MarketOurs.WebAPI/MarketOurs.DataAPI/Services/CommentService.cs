using MarketOurs.DataAPI.Configs;
using MarketOurs.Data.DataModels;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Repos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MarketOurs.DataAPI.Services;

public interface ICommentService
{
    Task<List<CommentDto>> GetAllAsync();
    Task<CommentDto?> GetByIdAsync(string id);
    Task<CommentDto?> CreateAsync(CommentCreateDto createDto);
    Task<CommentDto?> UpdateAsync(string id, CommentUpdateDto updateDto);
    Task DeleteAsync(string id);
    Task SetLikesAsync(string userId, string commentId);
    Task SetDislikesAsync(string userId, string commentId);
}

public class CommentService(
    ICommentRepo commentRepo, 
    IUserRepo userRepo, 
    ILikeManager likeManager,
    IMemoryCache memoryCache,
    IDistributedCache distributedCache,
    ILogger<CommentService> logger) : ICommentService
{
    private static readonly TimeSpan LocalCacheTtl = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan DistCacheTtl = TimeSpan.FromMinutes(10);

    public async Task<List<CommentDto>> GetAllAsync()
    {
        var comments = await commentRepo.GetAllAsync();
        var dtos = comments.Select(MapToDto).ToList();
        foreach (var dto in dtos)
        {
            dto.Likes = await likeManager.GetCommentLikesAsync(dto.Id, dto.Likes);
            dto.Dislikes = await likeManager.GetCommentDislikesAsync(dto.Id, dto.Dislikes);
        }
        return dtos;
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
            User = user,
            PostId = createDto.PostId,
            ParentCommentId = createDto.ParentCommentId,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            Likes = 0,
            Dislikes = 0
        };

        await commentRepo.CreateAsync(comment);
        // 清除该贴子的评论列表缓存（如果有）
        InvalidateCommentListCache(comment.PostId);
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
            PostId = comment.PostId,
            ParentCommentId = comment.ParentCommentId
        };
    }
}
