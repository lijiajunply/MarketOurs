using System.Security.Claims;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketOurs.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class CommentController(ICommentService commentService, ILogger<CommentController> logger) : ControllerBase
{
    /// <summary>
    /// 获取所有评论
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ApiResponse<List<CommentDto>>> GetAll()
    {
        var comments = await commentService.GetAllAsync();
        return ApiResponse<List<CommentDto>>.Success(comments, "获取评论列表成功");
    }

    /// <summary>
    /// 获取指定ID的评论
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ApiResponse<CommentDto>> GetById(string id)
    {
        var comment = await commentService.GetByIdAsync(id);
        if (comment == null)
        {
            return ApiResponse<CommentDto>.Fail(404, "评论不存在");
        }
        return ApiResponse<CommentDto>.Success(comment, "获取评论成功");
    }

    /// <summary>
    /// 创建评论
    /// </summary>
    [HttpPost]
    public async Task<ApiResponse<CommentDto>> Create([FromBody] CommentCreateDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return ApiResponse<CommentDto>.Fail(401, "未授权");
        }
        
        // 强制使用当前登录用户的ID
        request.UserId = userId;

        var comment = await commentService.CreateAsync(request);
        if (comment == null)
        {
            return ApiResponse<CommentDto>.Fail(400, "创建评论失败");
        }

        logger.LogInformation("用户 {UserId} 创建了评论 {CommentId}", userId, comment.Id);
        return ApiResponse<CommentDto>.Success(comment, "创建评论成功");
    }

    /// <summary>
    /// 回复评论
    /// </summary>
    [HttpPost("{id}/reply")]
    public async Task<ApiResponse<CommentDto>> Reply(string id, [FromBody] CommentCreateDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return ApiResponse<CommentDto>.Fail(401, "未授权");
        }
        
        var parentComment = await commentService.GetByIdAsync(id);
        if (parentComment == null)
        {
            return ApiResponse<CommentDto>.Fail(404, "要回复的评论不存在");
        }

        // 强制使用当前登录用户的ID，并设置父评论ID
        request.UserId = userId;
        request.ParentCommentId = id;
        request.PostId = parentComment.PostId; // 继承父评论的PostId

        var comment = await commentService.CreateAsync(request);
        if (comment == null)
        {
            return ApiResponse<CommentDto>.Fail(400, "回复评论失败");
        }

        logger.LogInformation("用户 {UserId} 回复了评论 {ParentCommentId}, 新评论ID: {CommentId}", userId, id, comment.Id);
        return ApiResponse<CommentDto>.Success(comment, "回复评论成功");
    }

    /// <summary>
    /// 更新评论
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ApiResponse<CommentDto>> Update(string id, [FromBody] CommentUpdateDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        // 验证要修改的评论是否属于当前用户或具有Admin权限
        var existingComment = await commentService.GetByIdAsync(id);
        if (existingComment == null)
        {
            return ApiResponse<CommentDto>.Fail(404, "评论不存在");
        }
        
        var isAdmin = User.IsInRole("Admin");
        if (existingComment.UserId != userId && !isAdmin)
        {
            return ApiResponse<CommentDto>.Fail(403, "无权修改他人的评论");
        }

        var updatedComment = await commentService.UpdateAsync(id, request);
        if (updatedComment == null)
        {
            return ApiResponse<CommentDto>.Fail(400, "更新评论失败");
        }

        logger.LogInformation("用户 {UserId} 更新了评论 {CommentId}", userId, id);
        return ApiResponse<CommentDto>.Success(updatedComment, "更新评论成功");
    }

    /// <summary>
    /// 删除评论
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ApiResponse> Delete(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        // 验证要删除的评论是否属于当前用户或具有Admin权限
        var existingComment = await commentService.GetByIdAsync(id);
        if (existingComment == null)
        {
            return ApiResponse.Fail(404, "评论不存在");
        }
        
        var isAdmin = User.IsInRole("Admin");
        if (existingComment.UserId != userId && !isAdmin)
        {
            return ApiResponse.Fail(403, "无权删除他人的评论");
        }

        await commentService.DeleteAsync(id);
        
        logger.LogInformation("用户 {UserId} 删除了评论 {CommentId}", userId, id);
        return ApiResponse.Success("删除评论成功");
    }

    /// <summary>
    /// 点赞评论
    /// </summary>
    [HttpPost("{id}/like")]
    public async Task<ApiResponse> Like(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return ApiResponse.Fail(401, "未授权");
        }

        await commentService.SetLikesAsync(userId, id);
        
        logger.LogInformation("用户 {UserId} 点赞了评论 {CommentId}", userId, id);
        return ApiResponse.Success("点赞成功");
    }

    /// <summary>
    /// 踩评论
    /// </summary>
    [HttpPost("{id}/dislike")]
    public async Task<ApiResponse> Dislike(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return ApiResponse.Fail(401, "未授权");
        }

        await commentService.SetDislikesAsync(userId, id);
        
        logger.LogInformation("用户 {UserId} 踩了评论 {CommentId}", userId, id);
        return ApiResponse.Success("操作成功");
    }
}