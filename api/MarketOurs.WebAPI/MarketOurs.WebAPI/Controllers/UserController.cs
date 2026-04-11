using System.Security.Claims;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Exceptions;
using MarketOurs.DataAPI.Services;
using MarketOurs.WebAPI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketOurs.WebAPI.Controllers;

/// <summary>
/// 用户控制器，提供管理员对用户的管理功能（增删改查）以及普通用户的个人资料维护
/// </summary>
[ApiController]
[Route("[controller]")]
public class UserController(IUserService userService, ILogger<UserController> logger) : ControllerBase
{
    #region Admin Operations (管理员操作)

    /// <summary>
    /// 获取所有注册用户 (仅限管理员，支持分页)
    /// </summary>
    /// <param name="params">分页参数</param>
    /// <returns>分页后的用户列表</returns>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResponse<PagedResultDto<UserDto>>> GetAllUsers([FromQuery] PaginationParams @params)
    {
        logger.LogInformation("Admin requested all users");
        var users = await userService.GetAllAsync(@params);
        return ApiResponse<PagedResultDto<UserDto>>.Success(users, "获取用户列表成功");
    }

    /// <summary>
    /// 根据关键词检索用户 (仅限管理员，支持分页)
    /// </summary>
    /// <param name="params">包含关键词的分页参数</param>
    /// <returns>匹配的用户分页列表</returns>
    [HttpGet("search")]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResponse<PagedResultDto<UserDto>>> Search([FromQuery] PaginationParams @params)
    {
        if (string.IsNullOrWhiteSpace(@params.Keyword))
        {
            return ApiResponse<PagedResultDto<UserDto>>.Success(PagedResultDto<UserDto>.Success([], 0, @params.PageIndex, @params.PageSize), "关键词不能为空");
        }

        var results = await userService.SearchAsync(@params);
        return ApiResponse<PagedResultDto<UserDto>>.Success(results, $"成功找到 {results.TotalCount} 条相关内容");
    }

    /// <summary>
    /// 根据用户 ID 获取详情 (仅限管理员)
    /// </summary>
    /// <param name="id">用户唯一标识</param>
    /// <returns>用户详细数据</returns>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResponse<UserDto>> GetUserById(string id)
    {
        logger.LogInformation("Admin requested user: {Id}", id);
        var user = await userService.GetByIdAsync(id);
        if (user == null) throw new ResourceAccessException(ErrorCode.UserNotFound, "用户不存在");
        return ApiResponse<UserDto>.Success(user, "获取用户成功");
    }

    /// <summary>
    /// 手动创建一个新用户 (仅限管理员)
    /// </summary>
    /// <param name="request">用户创建请求对象</param>
    /// <returns>创建成功的用户数据</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResponse<UserDto>> CreateUser([FromBody] UserCreateDto request)
    {
        logger.LogInformation("Admin creating user: {Account}, Role: {Role}", request.Account, request.Role);
        
        var existingUser = await userService.GetByAccountAsync(request.Account);
        if (existingUser != null) throw new BusinessException(ErrorCode.AccountAlreadyExists, "该账号已被注册", 409, "账号已存在");

        var user = await userService.CreateAsync(request);
        return ApiResponse<UserDto>.Success(user, "创建用户成功");
    }

    /// <summary>
    /// 更新指定用户的信息 (仅限管理员)
    /// </summary>
    /// <param name="id">目标用户 ID</param>
    /// <param name="request">用户信息更新请求对象</param>
    /// <returns>更新后的用户数据</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResponse<UserDto>> UpdateUserByAdmin(string id, [FromBody] UserUpdateDto request)
    {
        logger.LogInformation("Admin updating user: {Id}", id);
        var updatedUser = await userService.UpdateAsync(id, request);
        return ApiResponse<UserDto>.Success(updatedUser, "更新用户成功");
    }

    /// <summary>
    /// 删除指定用户 (仅限管理员，且不能删除自己)
    /// </summary>
    /// <param name="id">目标用户 ID</param>
    /// <returns>操作结果描述</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResponse> DeleteUser(string id)
    {
        logger.LogInformation("Admin deleting user: {Id}", id);
        
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == id)
        {
            throw new BusinessException(ErrorCode.OperationFailed, "不能删除当前登录的管理员账号");
        }

        var user = await userService.GetByIdAsync(id);
        if (user == null) throw new ResourceAccessException(ErrorCode.UserNotFound, "用户不存在");

        await userService.DeleteAsync(id);
        return ApiResponse.Success("删除用户成功");
    }

    #endregion

    #region Normal Operations (普通用户操作)

    /// <summary>
    /// 获取公开用户主页资料
    /// </summary>
    /// <param name="id">用户唯一标识</param>
    /// <returns>公开用户资料</returns>
    [HttpGet("public/{id}")]
    [AllowAnonymous]
    public async Task<ApiResponse<PublicUserProfileDto>> GetPublicProfileById(string id)
    {
        logger.LogInformation("Public profile requested: {Id}", id);
        var user = await userService.GetPublicProfileByIdAsync(id);
        if (user == null) throw new ResourceAccessException(ErrorCode.UserNotFound, "用户不存在");

        return ApiResponse<PublicUserProfileDto>.Success(user, "获取公开资料成功");
    }

    /// <summary>
    /// 获取当前登录用户的个人资料
    /// </summary>
    /// <returns>当前用户信息</returns>
    [HttpGet("profile")]
    [Authorize]
    [DataMasking]
    public async Task<ApiResponse<UserDto>> GetMyProfile()
    {
        var userId = this.GetRequiredUserId();

        var user = await userService.GetByIdAsync(userId);
        if (user == null) throw new ResourceAccessException(ErrorCode.UserNotFound, "用户不存在");

        return ApiResponse<UserDto>.Success(user, "获取个人资料成功");
    }

    /// <summary>
    /// 更新当前登录用户的个人资料 (如头像、简介、昵称)
    /// </summary>
    /// <param name="request">用户信息更新请求对象</param>
    /// <returns>更新后的用户信息</returns>
    [HttpPut("profile")]
    [Authorize]
    public async Task<ApiResponse<UserDto>> UpdateMyProfile([FromBody] UserUpdateDto request)
    {
        var userId = this.GetRequiredUserId();

        logger.LogInformation("User updating their profile: {Id}", userId);
        
        var updatedUser = await userService.UpdateAsync(userId, request);
        return ApiResponse<UserDto>.Success(updatedUser, "更新个人资料成功");
    }

    /// <summary>
    /// 修改当前登录用户的密码
    /// </summary>
    /// <param name="request">包含旧密码和新密码的请求对象</param>
    /// <returns>操作结果描述</returns>
    [HttpPut("password")]
    [Authorize]
    public async Task<ApiResponse> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = this.GetRequiredUserId();
        await userService.ChangePasswordAsync(userId, request.OldPassword, request.NewPassword);
        return ApiResponse.Success("密码修改成功");
    }

    /// <summary>
    /// 更新当前登录用户的移动端推送 Token (用于移动端消息推送)
    /// </summary>
    /// <param name="token">FCM 或其他平台的推送 Token</param>
    /// <returns>操作结果描述</returns>
    [HttpPost("push-token")]
    [Authorize]
    public async Task<ApiResponse> UpdatePushToken([FromBody] string token)
    {
        var userId = this.GetRequiredUserId();

        logger.LogInformation("User updating their push token: {Id}", userId);
        
        await userService.UpdatePushTokenAsync(userId, token);
        return ApiResponse.Success("更新成功");
    }

    #endregion
}
