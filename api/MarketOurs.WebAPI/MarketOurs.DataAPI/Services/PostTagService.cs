using MarketOurs.Data.DataModels;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Exceptions;
using MarketOurs.DataAPI.Repos;
using Microsoft.Extensions.Caching.Memory;

namespace MarketOurs.DataAPI.Services;

public interface IPostTagService
{
    Task<List<PostTagDto>> GetActiveAsync();
    Task<List<PostTagDto>> GetAllAsync();
    Task<PostTagDto> CreateAsync(PostTagCreateDto dto);
    Task<PostTagDto> UpdateAsync(string id, PostTagUpdateDto dto);
    Task<PostTagDto> DeactivateAsync(string id);
    Task<PostTagModel?> GetValidTagForPostAsync(string? tagId, bool allowInactive = false);
    void InvalidateCache();
}

public class PostTagService(IPostTagRepo postTagRepo, IMemoryCache memoryCache) : IPostTagService
{
    private const string ActiveCacheKey = "post_tags_active";
    private const string AllCacheKey = "post_tags_all";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<List<PostTagDto>> GetActiveAsync()
    {
        if (memoryCache.TryGetValue<List<PostTagDto>>(ActiveCacheKey, out var cached) && cached != null)
        {
            return cached;
        }

        var tags = (await postTagRepo.GetActiveAsync()).Select(MapToDto).ToList();
        memoryCache.Set(ActiveCacheKey, tags, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheTtl,
            Size = 1
        });
        return tags;
    }

    public async Task<List<PostTagDto>> GetAllAsync()
    {
        if (memoryCache.TryGetValue<List<PostTagDto>>(AllCacheKey, out var cached) && cached != null)
        {
            return cached;
        }

        var tags = (await postTagRepo.GetAllAsync()).Select(MapToDto).ToList();
        memoryCache.Set(AllCacheKey, tags, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheTtl,
            Size = 1
        });
        return tags;
    }

    public async Task<PostTagDto> CreateAsync(PostTagCreateDto dto)
    {
        var name = NormalizeName(dto.Name);
        var existing = await postTagRepo.GetByNameAsync(name);
        if (existing != null)
        {
            throw new BusinessException(ErrorCode.ResourceAlreadyExists, "标签名称已存在");
        }

        var now = DateTime.UtcNow;
        var tag = new PostTagModel
        {
            Name = name,
            Color = NormalizeColor(dto.Color),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        await postTagRepo.CreateAsync(tag);
        InvalidateCache();
        return MapToDto(tag);
    }

    public async Task<PostTagDto> UpdateAsync(string id, PostTagUpdateDto dto)
    {
        var tag = await postTagRepo.GetByIdAsync(id);
        if (tag == null) throw new ResourceAccessException(ErrorCode.InvalidStatusForOperation, "标签不存在");

        var name = NormalizeName(dto.Name);
        var existing = await postTagRepo.GetByNameAsync(name);
        if (existing != null && existing.Id != id)
        {
            throw new BusinessException(ErrorCode.ResourceAlreadyExists, "标签名称已存在");
        }

        tag.Name = name;
        tag.Color = NormalizeColor(dto.Color);
        tag.IsActive = dto.IsActive;
        tag.UpdatedAt = DateTime.UtcNow;

        await postTagRepo.UpdateAsync(tag);
        InvalidateCache();
        return MapToDto(tag);
    }

    public async Task<PostTagDto> DeactivateAsync(string id)
    {
        var tag = await postTagRepo.GetByIdAsync(id);
        if (tag == null) throw new ResourceAccessException(ErrorCode.InvalidStatusForOperation, "标签不存在");

        tag.IsActive = false;
        tag.UpdatedAt = DateTime.UtcNow;
        await postTagRepo.UpdateAsync(tag);
        InvalidateCache();
        return MapToDto(tag);
    }

    public async Task<PostTagModel?> GetValidTagForPostAsync(string? tagId, bool allowInactive = false)
    {
        if (string.IsNullOrWhiteSpace(tagId)) return null;

        var tag = await postTagRepo.GetByIdAsync(tagId.Trim());
        if (tag == null)
        {
            throw new ResourceAccessException(ErrorCode.InvalidStatusForOperation, "标签不存在", httpStatusCode: 400, resourceName: "PostTag", resourceId: tagId);
        }

        if (!allowInactive && !tag.IsActive)
        {
            throw new BusinessException(ErrorCode.InvalidStatusForOperation, "该标签已停用，不能用于新帖子");
        }

        return tag;
    }

    public void InvalidateCache()
    {
        memoryCache.Remove(ActiveCacheKey);
        memoryCache.Remove(AllCacheKey);
    }

    public static PostTagDto MapToDto(PostTagModel tag)
    {
        return new PostTagDto
        {
            Id = tag.Id,
            Name = tag.Name,
            Color = tag.Color,
            IsActive = tag.IsActive,
            CreatedAt = tag.CreatedAt,
            UpdatedAt = tag.UpdatedAt
        };
    }

    private static string NormalizeName(string name)
    {
        var normalized = name.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new BusinessException(ErrorCode.ParameterEmpty, "标签名称不能为空");
        }

        return normalized;
    }

    private static string NormalizeColor(string? color)
    {
        var normalized = color?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "#64748b" : normalized;
    }
}
