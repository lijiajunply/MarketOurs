using MarketOurs.DataAPI.Exceptions;
using MarketOurs.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketOurs.WebAPI.Controllers;

/// <summary>
/// IP黑名单管理控制器
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize(Roles = "Admin")]
public class IpBlacklistController(
    IIpBlacklistCacheService blacklistService,
    ILogger<IpBlacklistController> logger) : ControllerBase
{
    /// <summary>
    /// 获取黑名单统计信息
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<BlacklistStats>>> GetStats()
    {
        try
        {
            var stats = await blacklistService.GetStatsAsync();
            return Ok(ApiResponse<BlacklistStats>.Success(stats));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取黑名单统计信息时发生错误");
            return StatusCode(500, ApiResponse<BlacklistStats>.Fail(
                ErrorCode.InternalServerError, "获取统计信息失败"));
        }
    }

    /// <summary>
    /// 添加IP到黑名单
    /// </summary>
    /// <param name="request">添加请求</param>
    [HttpPost("add")]
    public async Task<ActionResult<ApiResponse<string>>> AddToBlacklist([FromBody] AddIpRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Ip))
            {
                return BadRequest(ApiResponse<string>.Fail(
                    ErrorCode.ParameterEmpty, "IP地址不能为空"));
            }

            await blacklistService.AddToBlacklistAsync(request.Ip);

            logger.LogInformation("管理员 {User} 添加IP到黑名单: {Ip}, 原因: {Reason}",
                User.Identity?.Name, request.Ip, request.Reason);

            return Ok(ApiResponse<string>.Success($"成功将 {request.Ip} 添加到黑名单"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<string>.Fail(
                ErrorCode.ParameterEmpty, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "添加IP到黑名单时发生错误: {Ip}", request.Ip);
            return StatusCode(500, ApiResponse<string>.Fail(
                ErrorCode.InternalServerError, "添加失败"));
        }
    }

    /// <summary>
    /// 从黑名单中移除IP
    /// </summary>
    /// <param name="request">移除请求</param>
    [HttpPost("remove")]
    public async Task<ActionResult<ApiResponse<string>>> RemoveFromBlacklist([FromBody] RemoveIpRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Ip))
            {
                return BadRequest(ApiResponse<string>.Fail(
                    ErrorCode.ParameterEmpty, "IP地址不能为空"));
            }

            await blacklistService.RemoveFromBlacklistAsync(request.Ip);

            logger.LogInformation("管理员 {User} 从黑名单移除IP: {Ip}, 原因: {Reason}",
                User.Identity?.Name, request.Ip, request.Reason);

            return Ok(ApiResponse<string>.Success($"成功从黑名单移除 {request.Ip}"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "从黑名单移除IP时发生错误: {Ip}", request.Ip);
            return StatusCode(500, ApiResponse<string>.Fail(
                ErrorCode.InternalServerError, "移除失败"));
        }
    }

    /// <summary>
    /// 刷新黑名单缓存
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<string>>> RefreshBlacklist()
    {
        try
        {
            await blacklistService.RefreshBlacklistAsync();

            logger.LogInformation("管理员 {User} 手动刷新黑名单缓存", User.Identity?.Name);

            return Ok(ApiResponse<string>.Success("黑名单缓存刷新成功"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "刷新黑名单缓存时发生错误");
            return StatusCode(500, ApiResponse<string>.Fail(
                ErrorCode.InternalServerError, "刷新失败"));
        }
    }

    /// <summary>
    /// 检查IP是否在黑名单中
    /// </summary>
    /// <param name="ip">IP地址</param>
    [HttpGet("check/{ip}")]
    public async Task<ActionResult<ApiResponse<IpCheckResult>>> CheckIp(string ip)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                return BadRequest(ApiResponse<IpCheckResult>.Fail(
                    ErrorCode.ParameterEmpty, "IP地址不能为空"));
            }

            var isBlacklisted = await blacklistService.IsIpBlacklistedAsync(ip);

            var result = new IpCheckResult
            {
                Ip = ip,
                IsBlacklisted = isBlacklisted,
                CheckTime = DateTime.UtcNow
            };

            return Ok(ApiResponse<IpCheckResult>.Success(result));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "检查IP时发生错误: {Ip}", ip);
            return StatusCode(500, ApiResponse<IpCheckResult>.Fail(
                ErrorCode.InternalServerError, "检查失败"));
        }
    }
}

/// <summary>
/// 添加IP请求
/// </summary>
public class AddIpRequest
{
    /// <summary>
    /// IP地址或CIDR范围（如：192.168.1.100 或 192.168.1.0/24）
    /// </summary>
    public string Ip { get; set; } = string.Empty;

    /// <summary>
    /// 添加原因
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// 移除IP请求
/// </summary>
public class RemoveIpRequest
{
    /// <summary>
    /// IP地址或CIDR范围
    /// </summary>
    public string Ip { get; set; } = string.Empty;

    /// <summary>
    /// 移除原因
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// IP检查结果
/// </summary>
public class IpCheckResult
{
    public string Ip { get; set; } = string.Empty;
    public bool IsBlacklisted { get; set; }
    public DateTime CheckTime { get; set; }
}