using MarketOurs.Data.DataModels;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Repos;

namespace MarketOurs.DataAPI.Services;

public interface IPostService
{
    Task<List<PostDto>> GetAllAsync();
    Task<PostDto?> GetByIdAsync(string id);
    Task<PostDto?> CreateAsync(PostCreateDto createDto);
    Task<PostDto?> UpdateAsync(string id, PostUpdateDto updateDto);
    Task DeleteAsync(string id);
    Task IncrementWatchAsync(string id);
    Task SetLikesAsync(string userId, string postId);
    Task SetDislikesAsync(string userId, string postId);
}

public class PostService(IPostRepo postRepo, IUserRepo userRepo, ILikeManager likeManager) : IPostService
{
    public async Task<List<PostDto>> GetAllAsync()
    {
        var posts = await postRepo.GetAllAsync();
        var dtos = posts.Select(MapToDto).ToList();
        foreach (var dto in dtos)
        {
            dto.Likes = await likeManager.GetPostLikesAsync(dto.Id, dto.Likes);
            dto.Dislikes = await likeManager.GetPostDislikesAsync(dto.Id, dto.Dislikes);
        }
        return dtos;
    }

    public async Task<PostDto?> GetByIdAsync(string id)
    {
        var post = await postRepo.GetByIdAsync(id);
        if (post == null) return null;
        
        var dto = MapToDto(post);
        dto.Likes = await likeManager.GetPostLikesAsync(dto.Id, dto.Likes);
        dto.Dislikes = await likeManager.GetPostDislikesAsync(dto.Id, dto.Dislikes);
        return dto;
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
        return MapToDto(post);
    }

    public async Task DeleteAsync(string id)
    {
        await postRepo.DeleteAsync(id);
    }

    public async Task IncrementWatchAsync(string id)
    {
        await postRepo.IncrementWatchAsync(id);
    }

    public async Task SetLikesAsync(string userId, string postId)
    {
        var post = await postRepo.GetByIdAsync(postId);
        if (post != null)
        {
            await likeManager.SetPostLikeAsync(postId, userId);
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
