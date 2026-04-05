using System.Security.Claims;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketOurs.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class PostController(IPostService postService) : ControllerBase
{
    /// <summary>
    /// 获取所有帖子 (分页)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ApiResponse<PagedResultDto<PostDto>>> GetAll([FromQuery] PaginationParams @params)
    {
        var posts = await postService.GetAllAsync(@params);
        return ApiResponse<PagedResultDto<PostDto>>.Success(posts, "获取成功");
    }

    /// <summary>
    /// 获取热门帖子
    /// </summary>
    [HttpGet("hot")]
    [AllowAnonymous]
    public async Task<ApiResponse<List<PostDto>>> GetHot([FromQuery] int count = 10)
    {
        var posts = await postService.GetHotAsync(count);
        return ApiResponse<List<PostDto>>.Success(posts, "获取成功");
    }

    /// <summary>
    /// 根据ID获取帖子详情，并增加浏览量
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ApiResponse<PostDto>> GetById(string id)
    {
        await postService.IncrementWatchAsync(id);
        var post = await postService.GetByIdAsync(id);
        return post == null ? ApiResponse<PostDto>.Fail(404, "帖子不存在") : ApiResponse<PostDto>.Success(post, "获取成功");
    }

    /// <summary>
    /// 创建帖子
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ApiResponse<PostDto>> Create([FromBody] PostCreateDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return ApiResponse<PostDto>.Fail(401, "未授权");
        }

        request.UserId = userId; // Ensure the user creates a post for themselves
        var post = await postService.CreateAsync(request);
        if (post == null)
        {
            return ApiResponse<PostDto>.Fail(500, "创建失败，用户可能不存在");
        }

        return ApiResponse<PostDto>.Success(post, "创建成功");
    }

    /// <summary>
    /// 更新帖子
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    public async Task<ApiResponse<PostDto>> Update(string id, [FromBody] PostUpdateDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return ApiResponse<PostDto>.Fail(401, "未授权");
        }

        var existingPost = await postService.GetByIdAsync(id);
        if (existingPost == null)
        {
            return ApiResponse<PostDto>.Fail(404, "帖子不存在");
        }

        var isAdmin = User.IsInRole("Admin");
        if (existingPost.UserId != userId && !isAdmin)
        {
            return ApiResponse<PostDto>.Fail(403, "无权修改他人的帖子");
        }

        var post = await postService.UpdateAsync(id, request);
        if (post == null)
        {
            return ApiResponse<PostDto>.Fail(500, "更新失败");
        }

        return ApiResponse<PostDto>.Success(post, "更新成功");
    }

    /// <summary>
    /// 删除帖子
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ApiResponse> Delete(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return ApiResponse.Fail(401, "未授权");
        }

        var existingPost = await postService.GetByIdAsync(id);
        if (existingPost == null)
        {
            return ApiResponse.Fail(404, "帖子不存在");
        }

        var isAdmin = User.IsInRole("Admin");
        if (existingPost.UserId != userId && !isAdmin)
        {
            return ApiResponse.Fail(403, "无权删除他人的帖子");
        }

        await postService.DeleteAsync(id);
        return ApiResponse.Success("删除成功");
    }

    /// <summary>
    /// 点赞帖子
    /// </summary>
    [HttpPost("{id}/like")]
    [Authorize]
    public async Task<ApiResponse> Like(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return ApiResponse.Fail(401, "未授权");
        }

        var existingPost = await postService.GetByIdAsync(id);
        if (existingPost == null)
        {
            return ApiResponse.Fail(404, "帖子不存在");
        }

        await postService.SetLikesAsync(userId, id);
        return ApiResponse.Success("操作成功");
    }

    /// <summary>
    /// 点踩帖子
    /// </summary>
    [HttpPost("{id}/dislike")]
    [Authorize]
    public async Task<ApiResponse> Dislike(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return ApiResponse.Fail(401, "未授权");
        }

        var existingPost = await postService.GetByIdAsync(id);
        if (existingPost == null)
        {
            return ApiResponse.Fail(404, "帖子不存在");
        }

        await postService.SetDislikesAsync(userId, id);
        return ApiResponse.Success("操作成功");
    }

    [HttpGet("{id}/comments/{type?}")]
    public async Task<ApiResponse<List<CommentDto>>> GetComments(string id, string? type)
    {
        return ApiResponse<List<CommentDto>>.Success(await postService.GetCommentsAsync(id, type ?? ""));
    }

    /// <summary>
    /// 全文检索帖子 (分页)
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ApiResponse<PagedResultDto<PostDto>>> Search([FromQuery] PaginationParams @params)
    {
        if (string.IsNullOrWhiteSpace(@params.Keyword))
        {
            return ApiResponse<PagedResultDto<PostDto>>.Success(PagedResultDto<PostDto>.Success([], 0, @params.PageIndex, @params.PageSize), "关键词不能为空");
        }

        var results = await postService.SearchAsync(@params);
        return ApiResponse<PagedResultDto<PostDto>>.Success(results, $"成功找到 {results.TotalCount} 条相关内容");
    }
}