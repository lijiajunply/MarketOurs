using System.Security.Claims;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketOurs.WebAPI.Controllers;

/// <summary>
/// 帖子控制器，提供帖子的增删改查、点赞/点踩、评论获取及全文搜索功能
/// </summary>
[ApiController]
[Route("[controller]")]
public class PostController(IPostService postService) : ControllerBase
{
    /// <summary>
    /// 获取所有帖子 (分页)
    /// </summary>
    /// <param name="params">分页与搜索参数</param>
    /// <returns>分页后的帖子列表</returns>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ApiResponse<PagedResultDto<PostDto>>> GetAll([FromQuery] PaginationParams @params)
    {
        var posts = await postService.GetAllAsync(@params);
        return ApiResponse<PagedResultDto<PostDto>>.Success(posts, "获取成功");
    }

    /// <summary>
    /// 获取指定用户发布的帖子 (分页)
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="params">分页参数</param>
    /// <returns>分页后的帖子列表</returns>
    [HttpGet("user/{userId}")]
    [AllowAnonymous]
    public async Task<ApiResponse<PagedResultDto<PostDto>>> GetByUserId(string userId, [FromQuery] PaginationParams @params)
    {
        var posts = await postService.GetByUserIdAsync(userId, @params);
        return ApiResponse<PagedResultDto<PostDto>>.Success(posts, "获取成功");
    }

    /// <summary>
    /// 获取热门帖子列表
    /// </summary>
    /// <param name="count">获取数量，默认为 10</param>
    /// <returns>热门帖子列表</returns>
    [HttpGet("hot")]
    [AllowAnonymous]
    public async Task<ApiResponse<List<PostDto>>> GetHot([FromQuery] int count = 10)
    {
        var posts = await postService.GetHotAsync(count);
        return ApiResponse<List<PostDto>>.Success(posts, "获取成功");
    }

    /// <summary>
    /// 根据 ID 获取帖子详情，并自动增加浏览量
    /// </summary>
    /// <param name="id">帖子唯一标识</param>
    /// <returns>帖子详细数据</returns>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ApiResponse<PostDto>> GetById(string id)
    {
        var post = await postService.GetByIdAsync(id);
        if (post == null)
        {
            return ApiResponse<PostDto>.Fail(404, "帖子不存在");
        }

        await postService.IncrementWatchAsync(id);
        post.Watch += 1;
        return ApiResponse<PostDto>.Success(post, "获取成功");
    }

    /// <summary>
    /// 创建新帖子 (需要登录)
    /// </summary>
    /// <param name="request">帖子创建请求对象</param>
    /// <returns>创建成功的帖子数据</returns>
    [HttpPost]
    [Authorize]
    public async Task<ApiResponse<PostDto>> Create([FromBody] PostCreateDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return ApiResponse<PostDto>.Fail(401, "未授权");
        }

        request.UserId = userId; // 强制将作者设置为当前登录用户
        var post = await postService.CreateAsync(request);
        if (post == null)
        {
            return ApiResponse<PostDto>.Fail(500, "创建失败，用户可能不存在");
        }

        return ApiResponse<PostDto>.Success(post, "创建成功，正在审核");
    }

    /// <summary>
    /// 更新帖子内容 (仅限作者或管理员)
    /// </summary>
    /// <param name="id">帖子 ID</param>
    /// <param name="request">帖子更新请求对象</param>
    /// <returns>更新后的帖子数据</returns>
    [HttpPut("{id}")]
    [Authorize]
    public async Task<ApiResponse<PostDto>> Update(string id, [FromBody] PostUpdateDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return ApiResponse<PostDto>.Fail(401, "未授权");
        }

        var existingPost = await postService.GetByIdIncludingPendingAsync(id);
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

        return ApiResponse<PostDto>.Success(post, "更新成功，正在重新审核");
    }

    /// <summary>
    /// 删除帖子 (仅限作者或管理员)
    /// </summary>
    /// <param name="id">帖子 ID</param>
    /// <returns>操作结果描述</returns>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ApiResponse> Delete(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return ApiResponse.Fail(401, "未授权");
        }

        var existingPost = await postService.GetByIdIncludingPendingAsync(id);
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
    /// 点赞帖子 (支持切换状态)
    /// </summary>
    /// <param name="id">帖子 ID</param>
    /// <returns>操作结果描述</returns>
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
    /// 点踩帖子 (支持切换状态)
    /// </summary>
    /// <param name="id">帖子 ID</param>
    /// <returns>操作结果描述</returns>
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

    /// <summary>
    /// 获取贴子的评论列表 (构造成树形结构)
    /// </summary>
    /// <param name="id">帖子 ID</param>
    /// <param name="type">排序类型: Hot (热门), New (最新，默认)</param>
    /// <returns>评论树列表</returns>
    [HttpGet("{id}/comments/{type?}")]
    public async Task<ApiResponse<List<CommentDto>>> GetComments(string id, string? type)
    {
        return ApiResponse<List<CommentDto>>.Success(await postService.GetCommentsAsync(id, type ?? ""));
    }

    /// <summary>
    /// 全文检索帖子内容 (支持分页)
    /// </summary>
    /// <param name="params">包含关键词的分页参数</param>
    /// <returns>相关帖子分页列表</returns>
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
