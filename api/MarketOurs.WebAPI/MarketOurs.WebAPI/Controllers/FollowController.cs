using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Exceptions;
using MarketOurs.DataAPI.Services;
using MarketOurs.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketOurs.WebAPI.Controllers;

/// <summary>
/// 关注控制器，提供用户关注和屏蔽功能
/// </summary>
[ApiController]
[Route("[controller]")]
public class FollowController(
    IFollowService followService,
    ILogger<FollowController> logger) : ControllerBase
{
    /// <summary>
    /// 切换关注状态（关注/取消关注）
    /// </summary>
    /// <param name="userId">目标用户ID</param>
    /// <returns>关注操作结果</returns>
    [HttpPost("users/{userId}")]
    [Authorize]
    public async Task<ApiResponse<FollowToggleResult>> ToggleFollow(string userId)
    {
        var followerId = this.GetRequiredUserId();

        if (followerId == userId)
        {
            throw new BusinessException(ErrorCode.OperationFailed, "不能关注自己");
        }

        logger.LogInformation("User {FollowerId} toggling follow for user {UserId}", followerId, userId);

        var result = await followService.ToggleFollowAsync(followerId, userId);

        return ApiResponse<FollowToggleResult>.Success(
            result,
            result.IsFollowing ? "关注成功" : "已取消关注"
        );
    }

    /// <summary>
    /// 获取用户的粉丝列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="params">分页参数</param>
    /// <returns>粉丝列表</returns>
    [HttpGet("users/{userId}/followers")]
    [AllowAnonymous]
    public async Task<ApiResponse<PagedResultDto<UserSimpleDto>>> GetFollowers(
        string userId,
        [FromQuery] PaginationParams @params)
    {
        logger.LogInformation("Fetching followers for user {UserId}", userId);

        var followers = await followService.GetFollowersAsync(userId, @params);

        return ApiResponse<PagedResultDto<UserSimpleDto>>.Success(followers, "获取粉丝列表成功");
    }

    /// <summary>
    /// 获取用户的关注列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="params">分页参数</param>
    /// <returns>关注列表</returns>
    [HttpGet("users/{userId}/following")]
    [AllowAnonymous]
    public async Task<ApiResponse<PagedResultDto<UserSimpleDto>>> GetFollowing(
        string userId,
        [FromQuery] PaginationParams @params)
    {
        logger.LogInformation("Fetching following for user {UserId}", userId);

        var following = await followService.GetFollowingAsync(userId, @params);

        return ApiResponse<PagedResultDto<UserSimpleDto>>.Success(following, "获取关注列表成功");
    }

    /// <summary>
    /// 屏蔽用户
    /// </summary>
    /// <param name="userId">要屏蔽的用户ID</param>
    /// <returns>操作结果</returns>
    [HttpPost("block/{userId}")]
    [Authorize]
    public async Task<ApiResponse> BlockUser(string userId)
    {
        var blockerId = this.GetRequiredUserId();

        if (blockerId == userId)
        {
            throw new BusinessException(ErrorCode.OperationFailed, "不能屏蔽自己");
        }

        logger.LogInformation("User {BlockerId} blocking user {UserId}", blockerId, userId);

        await followService.BlockUserAsync(blockerId, userId);

        return ApiResponse.Success("屏蔽成功");
    }

    /// <summary>
    /// 取消屏蔽用户
    /// </summary>
    /// <param name="userId">要取消屏蔽的用户ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("block/{userId}")]
    [Authorize]
    public async Task<ApiResponse> UnblockUser(string userId)
    {
        var blockerId = this.GetRequiredUserId();

        logger.LogInformation("User {BlockerId} unblocking user {UserId}", blockerId, userId);

        await followService.UnblockUserAsync(blockerId, userId);

        return ApiResponse.Success("已取消屏蔽");
    }

    /// <summary>
    /// 获取当前用户的屏蔽列表
    /// </summary>
    /// <param name="params">分页参数</param>
    /// <returns>屏蔽用户列表</returns>
    [HttpGet("block")]
    [Authorize]
    public async Task<ApiResponse<PagedResultDto<UserSimpleDto>>> GetBlockedUsers(
        [FromQuery] PaginationParams @params)
    {
        var userId = this.GetRequiredUserId();

        logger.LogInformation("Fetching blocked users for {UserId}", userId);

        var blocked = await followService.GetBlockedUsersAsync(userId, @params);

        return ApiResponse<PagedResultDto<UserSimpleDto>>.Success(blocked, "获取屏蔽列表成功");
    }
}
