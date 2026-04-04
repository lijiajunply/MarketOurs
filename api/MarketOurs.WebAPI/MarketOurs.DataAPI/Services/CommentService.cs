using MarketOurs.Data.DataModels;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Repos;

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


public class CommentService(ICommentRepo commentRepo, IUserRepo userRepo, ILikeManager likeManager) : ICommentService
{
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
        var comment = await commentRepo.GetByIdAsync(id);
        if (comment == null) return null;
        
        var dto = MapToDto(comment);
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
        return MapToDto(comment);
    }

    public async Task DeleteAsync(string id)
    {
        await commentRepo.DeleteAsync(id);
    }

    public async Task SetLikesAsync(string userId, string commentId)
    {
        var comment = await commentRepo.GetByIdAsync(commentId);
        if (comment != null)
        {
            await likeManager.SetCommentLikeAsync(commentId, userId);
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
