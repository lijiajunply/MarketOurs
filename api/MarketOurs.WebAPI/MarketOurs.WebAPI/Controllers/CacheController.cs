using System.Text;
using MarketOurs.DataAPI.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace MarketOurs.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(Roles = "Admin")]
public class CacheController(
    IConnectionMultiplexer redis,
    ILogger<CacheController> logger) : ControllerBase
{
    private readonly IDatabase _db = redis.GetDatabase();

    [HttpGet("/clean")]
    public async Task<ActionResult<ApiResponse<string>>> CleanData()
    {
        try
        {
            // 获取Redis服务器实例
            var server = redis.GetServer(redis.GetEndPoints().First());

            // 定义本项目使用的缓存键前缀
            var projectKeyPrefixes = new[]
            {
                "token:",
                "refresh:",
                "user:",
                "oauth:auth:",
                "blacklist:refresh:"
            };

            // 查找所有键
            var keys = server.Keys(pattern: "*", pageSize: 1000);

            // 过滤出本项目生成的缓存项
            var projectKeys = keys.Where(key => projectKeyPrefixes.Any(prefix => key.ToString().StartsWith(prefix)))
                .ToArray();
            var builder = new StringBuilder();
            foreach (var key in projectKeys)
            {
                builder.AppendLine(key);
            }

            // 删除所有本项目的缓存项并获取删除的数量
            long deletedCount = 0;
            if (projectKeys.Length > 0)
            {
                deletedCount = await _db.KeyDeleteAsync(projectKeys);
            }

            var message = $"缓存已清除，共清理了 {deletedCount} 个缓存项 \n\r 清理的缓存项列表：\n\r {builder}";
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("缓存已清除，共清理了 {DeletedCount} 个缓存项", deletedCount);
            }

            return Ok(ApiResponse<string>.Success(message, "缓存清除成功"));
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(ex, "清除缓存时出现问题");
            }

            return Ok(ApiResponse<string>.Fail(ErrorCode.CacheOperationFailed, $"清除缓存时出现问题: {ex.Message}"));
        }
    }
}