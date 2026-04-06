using System.Security.Claims;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketOurs.WebAPI.Controllers;

/// <summary>
/// 评论控制器，提供评论的获取、创建、回复、更新、删除以及点赞/点踩功能
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public class CommentController(ICommentService commentService, ILogger<CommentController> logger) : ControllerBase
{
    /// <summary>
    /// 获取所有评论 (分页)
    /// </summary>
    /// <param name="params">分页参数</param>
    /// <returns>分页后的评论列表</returns>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ApiResponse<PagedResultDto<CommentDto>>> GetAll([FromQuery] PaginationParams @params)
    {
        var comments = await commentService.GetAllAsync(@params);
        return ApiResponse<PagedResultDto<CommentDto>>.Success(comments, "获取评论列表成功");
    }

    /// <summary>
    /// 全文检索评论内容 (分页)
    /// </summary>
    /// <param name="params">包含关键词的分页参数</param>
    /// <returns>相关评论分页列表</returns>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ApiResponse<PagedResultDto<CommentDto>>> Search([FromQuery] PaginationParams @params)
    {
        if (string.IsNullOrWhiteSpace(@params.Keyword))
        {
            return ApiResponse<PagedResultDto<CommentDto>>.Success(PagedResultDto<CommentDto>.Success([], 0, @params.PageIndex, @params.PageSize), "关键词不能为空");
        }

        var results = await commentService.SearchAsync(@params);
        return ApiResponse<PagedResultDto<CommentDto>>.Success(results, $"成功找到 {results.TotalCount} 条相关内容");
    }

    /// <summary>
    /// 根据 ID 获取单个评论详情
    /// </summary>
    /// <param name="id">评论唯一标识</param>
    /// <returns>评论详细数据</returns>
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
    /// 创建新评论 (主评论，不带 ParentId)
    /// </summary>
    /// <param name="request">评论创建请求对象</param>
    /// <returns>创建成功的评论数据</returns>
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
    /// 回复现有评论
    /// </summary>
    /// <param name="id">被回复的父评论 ID</param>
    /// <param name="request">评论创建请求对象</param>
    /// <returns>创建成功的回复评论数据</returns>
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
    /// 更新评论内容 (仅限作者或管理员)
    /// </summary>
    /// <param name="id">评论 ID</param>
    /// <param name="request">评论更新请求对象</param>
    /// <returns>更新后的评论数据</returns>
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
    /// 删除评论 (仅限作者或管理员)
    /// </summary>
    /// <param name="id">评论 ID</param>
    /// <returns>操作结果描述</returns>
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
    /// 点赞评论 (支持切换状态)
    /// </summary>
    /// <param name="id">评论 ID</param>
    /// <returns>操作结果描述</returns>
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
    /// 点踩评论 (支持切换状态)
    /// </summary>
    /// <param name="id">评论 ID</param>
    /// <returns>操作结果描述</returns>
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