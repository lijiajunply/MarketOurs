using MarketOurs.Data;
using MarketOurs.Data.DataModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MarketOurs.DataAPI.Repos;

public interface IAdminRepo
{
    Task<int> GetTotalUsersAsync();
    Task<int> GetActiveUsersAsync();
    Task<int> GetTotalPostsAsync();
    Task<int> GetPostsCreatedSinceAsync(DateTime since);
    Task<UserModel?> GetUserByIdAsync(string id);
    Task UpdateUserAsync(UserModel user);
    Task<Dictionary<DateTime, int>> GetDailyPostCountsAsync(DateTime startDate, DateTime endDate);
    Task<Dictionary<DateTime, int>> GetDailyUserCountsAsync(DateTime startDate, DateTime endDate);
    Task<List<(string Id, string Type, string Title, string Description, DateTime Timestamp)>> GetRecentActivitiesAsync();
    Task<(int TotalLogs, int ErrorLogs)> GetLogStatsAsync();
    Task<(int BlacklistHits, int CacheHits)> GetBlacklistAndCacheStatsAsync();
}

public class AdminRepo(
    IDbContextFactory<MarketContext> dbContextFactory,
    ILogger<AdminRepo> logger) : IAdminRepo
{
    private const string LogConnectionString = "Data Source=logs/log.db";

    public async Task<int> GetTotalUsersAsync()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Users.CountAsync();
    }

    public async Task<int> GetActiveUsersAsync()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Users.CountAsync(user => user.IsActive);
    }

    public async Task<int> GetTotalPostsAsync()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Posts.CountAsync();
    }

    public async Task<int> GetPostsCreatedSinceAsync(DateTime since)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Posts.CountAsync(post => post.CreatedAt >= since);
    }

    public async Task<UserModel?> GetUserByIdAsync(string id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Users.FirstOrDefaultAsync(item => item.Id == id);
    }

    public async Task UpdateUserAsync(UserModel user)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        context.Users.Update(user);
        await context.SaveChangesAsync();
    }

    public async Task<Dictionary<DateTime, int>> GetDailyPostCountsAsync(DateTime startDate, DateTime endDate)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var counts = await context.Posts
            .Where(post => post.CreatedAt >= startDate && post.CreatedAt < endDate.AddDays(1))
            .GroupBy(post => post.CreatedAt.Date)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToListAsync();

        return counts.ToDictionary(item => item.Key, item => item.Count);
    }

    public async Task<Dictionary<DateTime, int>> GetDailyUserCountsAsync(DateTime startDate, DateTime endDate)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var counts = await context.Users
            .Where(user => user.CreatedAt >= startDate && user.CreatedAt < endDate.AddDays(1))
            .GroupBy(user => user.CreatedAt.Date)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToListAsync();

        return counts.ToDictionary(item => item.Key, item => item.Count);
    }

    public async Task<List<(string Id, string Type, string Title, string Description, DateTime Timestamp)>> GetRecentActivitiesAsync()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var recentUsers = await context.Users
            .OrderByDescending(user => user.CreatedAt)
            .Take(3)
            .Select(user => new
            {
                Id = $"user-{user.Id}",
                Type = "user",
                Title = "新用户注册",
                Description = $"{user.Name} 已加入 MarketOurs",
                Timestamp = user.CreatedAt
            })
            .ToListAsync();

        var recentPosts = await context.Posts
            .Include(post => post.User)
            .OrderByDescending(post => post.CreatedAt)
            .Take(3)
            .Select(post => new
            {
                Id = $"post-{post.Id}",
                Type = "post",
                Title = "新帖子发布",
                Description = $"{post.User.Name} 发布了《{post.Title}》",
                Timestamp = post.CreatedAt
            })
            .ToListAsync();

        return recentUsers
            .Concat(recentPosts)
            .OrderByDescending(item => item.Timestamp)
            .Take(6)
            .Select(item => (item.Id, item.Type, item.Title, item.Description, item.Timestamp))
            .ToList();
    }

    public async Task<(int TotalLogs, int ErrorLogs)> GetLogStatsAsync()
    {
        try
        {
            await using var connection = new SqliteConnection(LogConnectionString);
            await connection.OpenAsync();

            var totalCommand = connection.CreateCommand();
            totalCommand.CommandText = "SELECT COUNT(*) FROM Logs";
            var totalLogs = Convert.ToInt32(await totalCommand.ExecuteScalarAsync());

            var errorCommand = connection.CreateCommand();
            errorCommand.CommandText = "SELECT COUNT(*) FROM Logs WHERE Level = 'Error' OR Level = 'Fatal'";
            var errorLogs = Convert.ToInt32(await errorCommand.ExecuteScalarAsync());

            return (totalLogs, errorLogs);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "读取日志统计失败");
            return (0, 0);
        }
    }

    public async Task<(int BlacklistHits, int CacheHits)> GetBlacklistAndCacheStatsAsync()
    {
        try
        {
            await using var connection = new SqliteConnection(LogConnectionString);
            await connection.OpenAsync();

            var blacklistCommand = connection.CreateCommand();
            blacklistCommand.CommandText = """
                                           SELECT COUNT(*) FROM Logs
                                           WHERE RenderedMessage LIKE '%blacklist%'
                                              OR RenderedMessage LIKE '%黑名单%'
                                           """;
            var blacklistHits = Convert.ToInt32(await blacklistCommand.ExecuteScalarAsync());

            var cacheCommand = connection.CreateCommand();
            cacheCommand.CommandText = """
                                       SELECT COUNT(*) FROM Logs
                                       WHERE RenderedMessage LIKE '%cache hit%'
                                          OR RenderedMessage LIKE '%缓存命中%'
                                       """;
            var cacheHits = Convert.ToInt32(await cacheCommand.ExecuteScalarAsync());

            return (blacklistHits, cacheHits);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "读取系统命中统计失败");
            return (0, 0);
        }
    }
}
