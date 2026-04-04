using System.Security.Claims;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketOurs.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController(IUserService userService, ILogger<UserController> logger) : ControllerBase
{
    #region Admin Operations

    /// <summary>
    /// 获取所有用户 (Admin)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResponse<List<UserDto>>> GetAllUsers()
    {
        logger.LogInformation("Admin requested all users");
        var users = await userService.GetAllAsync();
        return ApiResponse<List<UserDto>>.Success(users, "获取用户列表成功");
    }

    /// <summary>
    /// 根据ID获取用户 (Admin)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResponse<UserDto>> GetUserById(string id)
    {
        logger.LogInformation("Admin requested user: {Id}", id);
        var user = await userService.GetByIdAsync(id);
        if (user == null)
        {
            return ApiResponse<UserDto>.Fail(404, "用户不存在");
        }
        return ApiResponse<UserDto>.Success(user, "获取用户成功");
    }

    /// <summary>
    /// 创建用户 (Admin)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResponse<UserDto>> CreateUser([FromBody] UserCreateDto request)
    {
        logger.LogInformation("Admin creating user: {Email}, Role: {Role}", request.Email, request.Role);
        
        var existingUser = await userService.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return ApiResponse<UserDto>.Fail(400, "该邮箱已被注册");
        }

        var user = await userService.CreateAsync(request);
        return ApiResponse<UserDto>.Success(user, "创建用户成功");
    }

    /// <summary>
    /// 更新指定用户信息 (Admin)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResponse<UserDto>> UpdateUserByAdmin(string id, [FromBody] UserUpdateDto request)
    {
        logger.LogInformation("Admin updating user: {Id}", id);
        var updatedUser = await userService.UpdateAsync(id, request);
        if (updatedUser == null)
        {
            return ApiResponse<UserDto>.Fail(404, "用户不存在");
        }
        return ApiResponse<UserDto>.Success(updatedUser, "更新用户成功");
    }

    /// <summary>
    /// 删除用户 (Admin)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResponse> DeleteUser(string id)
    {
        logger.LogInformation("Admin deleting user: {Id}", id);
        
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == id)
        {
            return ApiResponse.Fail(400, "不能删除当前登录的管理员账号");
        }

        var user = await userService.GetByIdAsync(id);
        if (user == null)
        {
            return ApiResponse.Fail(404, "用户不存在");
        }

        await userService.DeleteAsync(id);
        return ApiResponse.Success("删除用户成功");
    }

    #endregion

    #region Normal Operations

    /// <summary>
    /// 获取当前登录用户信息 (普通操作)
    /// </summary>
    [HttpGet("profile")]
    [Authorize]
    public async Task<ApiResponse<UserDto>> GetMyProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return ApiResponse<UserDto>.Fail(401, "未授权");
        }

        var user = await userService.GetByIdAsync(userId);
        if (user == null)
        {
            return ApiResponse<UserDto>.Fail(404, "用户不存在");
        }

        return ApiResponse<UserDto>.Success(user, "获取个人资料成功");
    }

    /// <summary>
    /// 更新当前登录用户个人信息 (普通操作)
    /// </summary>
    [HttpPut("profile")]
    [Authorize]
    public async Task<ApiResponse<UserDto>> UpdateMyProfile([FromBody] UserUpdateDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return ApiResponse<UserDto>.Fail(401, "未授权");
        }

        logger.LogInformation("User updating their profile: {Id}", userId);
        
        var updatedUser = await userService.UpdateAsync(userId, request);
        if (updatedUser == null)
        {
            return ApiResponse<UserDto>.Fail(404, "用户不存在");
        }

        return ApiResponse<UserDto>.Success(updatedUser, "更新个人资料成功");
    }

    #endregion
}
