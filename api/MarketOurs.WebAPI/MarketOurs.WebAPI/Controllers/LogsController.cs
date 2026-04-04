using System.Data.SQLite;
using System.Text.Json;
using MarketOurs.DataAPI.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace MarketOurs.WebAPI.Controllers;

/// <summary>
/// 日志控制器，提供日志查询和过滤功能
/// 可以获取最近的日志条目或按级别过滤日志
/// </summary>
[ApiController]
[Authorize]
[Route("[controller]")]
public class LogsController(ILogger<LogsController> logger)
    : ControllerBase
{
    /// <summary>
    /// SQLite数据库连接字符串，用于访问日志数据库
    /// </summary>
    private const string ConnectionString = "Data Source=logs/log.db";

    // 从配置获取连接字符串或使用默认值

    /// <summary>
    /// 获取最近的日志条目，支持分页和多条件搜索
    /// </summary>
    /// <remarks>
    /// 此API提供了三种搜索模式，可以单独使用或组合使用：
    /// 1. 名称/内容搜索：通过关键词在日志消息和属性中进行模糊匹配
    /// 2. 日志级别过滤：精确匹配指定的日志级别
    /// 3. 时间范围过滤：支持"today"（当天）或指定天数（如"7"表示最近7天）
    /// 
    /// 所有搜索条件之间使用AND逻辑进行组合。
    /// </remarks>
    /// <param name="pageIndex">页码，从1开始，默认为1</param>
    /// <param name="pageSize">每页记录数，默认为10，最大不超过100</param>
    /// <param name="searchTerm">搜索关键词，用于在日志消息和属性中搜索（最多100个字符）</param>
    /// <param name="levelFilter">日志级别过滤，如"Information"、"Warning"、"Error"等</param>
    /// <param name="timeRange">时间范围过滤："today"表示当天，或正整数表示最近几天</param>
    /// <returns>包含分页信息和日志条目的JSON响应</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<LogEntry>>>> GetRecentLogs(int pageIndex = 1,
        int pageSize = 10, string? searchTerm = null,
        string? levelFilter = null, string? timeRange = null)
    {
        try
        {
            var logs = new List<LogEntry>();
            int totalCount;

            await using (var connection = new SqliteConnection(ConnectionString))
            {
                await connection.OpenAsync();

                // 确保参数有效
                if (pageIndex < 1) pageIndex = 1;
                if (pageSize is < 1 or > 100) pageSize = 10;

                // 搜索参数验证
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    // 限制搜索关键词长度
                    if (searchTerm.Length > 100)
                    {
                        searchTerm = searchTerm.Substring(0, 100);
                    }

                    // 去除前后空白
                    searchTerm = searchTerm.Trim();
                }

                // 日志级别验证 - 可选，取决于系统支持的日志级别
                if (!string.IsNullOrEmpty(levelFilter))
                {
                    // 转换为标准格式
                    levelFilter = levelFilter.Trim();
                }

                // 时间范围验证
                if (!string.IsNullOrEmpty(timeRange))
                {
                    timeRange = timeRange.Trim();
                    // 如果不是"today"，则验证是否为有效的天数
                    if (!timeRange.Equals("today", StringComparison.OrdinalIgnoreCase))
                    {
                        // 尝试解析为天数，如果解析失败或天数小于等于0，则设为null
                        if (!int.TryParse(timeRange, out int days) || days <= 0)
                        {
                            timeRange = null;
                        }
                    }
                }

                // 构建基础查询和参数
                var conditions = new List<string>();

                // 名称/内容搜索
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    conditions.Add("(RenderedMessage LIKE @SearchTerm OR Properties LIKE @SearchTerm)");
                }

                // 级别搜索
                if (!string.IsNullOrEmpty(levelFilter))
                {
                    conditions.Add("Level = @LevelFilter");
                }

                // 时间差搜索
                if (!string.IsNullOrEmpty(timeRange))
                {
                    if (timeRange.Equals("today", StringComparison.OrdinalIgnoreCase))
                    {
                        conditions.Add("Timestamp >= @TodayStart");
                    }
                    else if (int.TryParse(timeRange, out _))
                    {
                        conditions.Add("Timestamp >= @DateThreshold");
                    }
                }

                // 构建WHERE子句
                var whereClause = conditions.Count > 0 ? " WHERE " + string.Join(" AND ", conditions) : "";

                // 获取总日志数量（带过滤条件）
                var countCommand = connection.CreateCommand();
                countCommand.CommandText = $"SELECT COUNT(*) FROM Logs{whereClause}";

                // 设置参数
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    countCommand.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");
                }

                if (!string.IsNullOrEmpty(levelFilter))
                {
                    countCommand.Parameters.AddWithValue("@LevelFilter", levelFilter);
                }

                if (!string.IsNullOrEmpty(timeRange))
                {
                    if (timeRange.Equals("today", StringComparison.OrdinalIgnoreCase))
                    {
                        countCommand.Parameters.AddWithValue("@TodayStart", DateTime.Today);
                    }
                    else if (int.TryParse(timeRange, out int days))
                    {
                        countCommand.Parameters.AddWithValue("@DateThreshold", DateTime.Now.AddDays(-days));
                    }
                }

                totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());

                // 构建分页查询
                var command = connection.CreateCommand();
                command.CommandText = $"""
                                       SELECT Timestamp, Level, Exception, Properties, RenderedMessage
                                       FROM Logs
                                       {whereClause}
                                       ORDER BY Timestamp DESC 
                                       LIMIT @PageSize OFFSET @Offset
                                       """;

                // 设置查询参数
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    command.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");
                }

                if (!string.IsNullOrEmpty(levelFilter))
                {
                    command.Parameters.AddWithValue("@LevelFilter", levelFilter);
                }

                if (!string.IsNullOrEmpty(timeRange))
                {
                    if (timeRange.Equals("today", StringComparison.OrdinalIgnoreCase))
                    {
                        command.Parameters.AddWithValue("@TodayStart", DateTime.Today);
                    }
                    else if (int.TryParse(timeRange, out int days))
                    {
                        command.Parameters.AddWithValue("@DateThreshold", DateTime.Now.AddDays(-days));
                    }
                }

                command.Parameters.AddWithValue("@PageSize", pageSize);
                command.Parameters.AddWithValue("@Offset", (pageIndex - 1) * pageSize);

                await using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        logs.Add(new LogEntry
                        {
                            Timestamp = reader.GetDateTime(0),
                            Level = reader.GetString(1),
                            Exception = reader.IsDBNull(2) ? null : reader.GetString(2),
                            Properties = reader.IsDBNull(3)
                                ? null
                                : JsonSerializer.Deserialize<Dictionary<string, object>>(reader.GetString(3)),
                            Message = reader.IsDBNull(4) ? null : reader.GetString(4),
                        });
                    }
                }
            }

            // 计算总页数
            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            // 创建分页响应
            var response = new PaginatedResponse<LogEntry>
            {
                Data = logs,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalPages = totalPages
            };

            return Ok(ApiResponse<PaginatedResponse<LogEntry>>.Success(response, "获取日志成功"));
        }
        catch (Exception ex)
        {
            // 记录包含搜索参数的详细错误日志，便于调试
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex,
                    "获取日志时发生错误，搜索参数：searchTerm={SearchTerm}, levelFilter={LevelFilter}, timeRange={TimeRange}",
                    searchTerm ?? "null", levelFilter ?? "null", timeRange ?? "null");
            }

            return Ok(ApiResponse<PaginatedResponse<LogEntry>>.Fail(ErrorCode.InternalServerError,
                $"获取日志时发生错误: {ex.Message}"));
        }
    }


    /// <summary>
    /// 获取日志统计信息，包括总日志数量和各日志级别数量
    /// </summary>
    /// <returns>日志统计结果</returns>
    [HttpGet("statistics")]
    public async Task<ActionResult<ApiResponse<LogStatistics>>> GetLogStatistics()
    {
        try
        {
            var statistics = new LogStatistics();

            await using (var connection = new SqliteConnection(ConnectionString))
            {
                await connection.OpenAsync();

                // 获取总日志数量
                var totalCommand = connection.CreateCommand();
                totalCommand.CommandText = "SELECT COUNT(*) FROM Logs";
                statistics.TotalCount = Convert.ToInt32(await totalCommand.ExecuteScalarAsync());

                // 获取各日志级别的数量
                var levelCommand = connection.CreateCommand();
                levelCommand.CommandText = "SELECT Level, COUNT(*) FROM Logs GROUP BY Level";

                await using (var reader = await levelCommand.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var level = reader.GetString(0);
                        var count = reader.GetInt32(1);
                        statistics.LevelCounts[level] = count;
                    }
                }
            }

            return Ok(ApiResponse<LogStatistics>.Success(statistics, "获取日志统计信息成功"));
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "获取日志统计信息时发生错误");
            }

            return Ok(ApiResponse<LogStatistics>.Fail(ErrorCode.InternalServerError, $"获取日志统计信息时发生错误: {ex.Message}"));
        }
    }

    /// <summary>
    /// 手动清理旧日志
    /// </summary>
    /// <param name="days">要保留的日志天数，默认为7天</param>
    /// <returns>清理结果</returns>
    [HttpPost("cleanup")]
    public async Task<ActionResult<ApiResponse<object>>> CleanupOldLogs([FromQuery] int days = 7)
    {
        try
        {
            // 验证参数
            if (days <= 0)
            {
                return Ok(ApiResponse<object>.Fail(ErrorCode.ParameterOutOfRange, "天数必须大于0"));
            }

            // 获取日志数据库路径
            var sqlPath = "logs/log.db";
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production")
            {
                sqlPath = Environment.CurrentDirectory + "/logs/log.db";
            }

            // 清理指定天数前的日志
            await using var connection = new SQLiteConnection($"Data Source={sqlPath}");
            await connection.OpenAsync();
            await using var command =
                new SQLiteCommand("DELETE FROM Logs WHERE Timestamp < @cutoffDate", connection);
            command.Parameters.AddWithValue("@cutoffDate", DateTime.Now.AddDays(-days));
            var rowsAffected = await command.ExecuteNonQueryAsync();

            var result = new { Message = $"成功清理了 {rowsAffected} 条 {days} 天前的日志" };
            return Ok(ApiResponse<object>.Success(result, "清理日志成功"));
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "手动清理旧日志时出错");
            }

            return Ok(ApiResponse<object>.Fail(ErrorCode.InternalServerError, $"清理旧日志时出错: {ex.Message}"));
        }
    }

    /// <summary>
    /// 获取日志分布数据，用于图表展示
    /// </summary>
    /// <param name="timeRange">时间范围过滤："today"表示当天，或正整数表示最近几天</param>
    /// <returns>按时间分布的日志数据</returns>
    [HttpGet("distribution")]
    public async Task<ActionResult<ApiResponse<List<LogDistribution>>>> GetLogDistribution(string? timeRange = "today")
    {
        try
        {
            var distributions = new List<LogDistribution>();

            await using (var connection = new SqliteConnection(ConnectionString))
            {
                await connection.OpenAsync();

                // 时间范围验证和处理
                if (!string.IsNullOrEmpty(timeRange))
                {
                    timeRange = timeRange.Trim();
                    // 如果不是"today"，则验证是否为有效的天数
                    if (!timeRange.Equals("today", StringComparison.OrdinalIgnoreCase))
                    {
                        // 尝试解析为天数，如果解析失败或天数小于等于0，则设为"today"
                        if (!int.TryParse(timeRange, out int days) || days <= 0)
                        {
                            timeRange = "today";
                        }
                    }
                }
                else
                {
                    timeRange = "today";
                }

                // 构建时间过滤条件
                string timeFilter = string.Empty;
                if (timeRange.Equals("today", StringComparison.OrdinalIgnoreCase))
                {
                    timeFilter = "Timestamp >= @TodayStart";
                }
                else if (int.TryParse(timeRange, out int days))
                {
                    timeFilter = "Timestamp >= @DateThreshold";
                }

                // 根据时间范围选择不同的分组粒度
                var groupByFormat = timeRange.Equals("today", StringComparison.OrdinalIgnoreCase)
                    ? "strftime('%H:00', Timestamp)" // 当天数据按小时分组
                    : "strftime('%Y-%m-%d', Timestamp)"; // 多天数据按天分组

                // 构建查询
                var query = $@"
                    SELECT 
                        {groupByFormat} as TimePoint,
                        COUNT(*) as TotalCount,
                        SUM(CASE WHEN Level = 'Error' OR Level = 'Critical' THEN 1 ELSE 0 END) as ErrorCount,
                        SUM(CASE WHEN Level = 'Information' THEN 1 ELSE 0 END) as InfoCount,
                        SUM(CASE WHEN Level = 'Warning' THEN 1 ELSE 0 END) as WarningCount
                    FROM Logs
                    WHERE {timeFilter}
                    GROUP BY TimePoint
                    ORDER BY TimePoint;
                ";

                await using var command = new SqliteCommand(query, connection);

                // 设置参数
                if (timeRange.Equals("today", StringComparison.OrdinalIgnoreCase))
                {
                    command.Parameters.AddWithValue("@TodayStart", DateTime.Today);
                }
                else if (int.TryParse(timeRange, out int days))
                {
                    command.Parameters.AddWithValue("@DateThreshold", DateTime.Now.AddDays(-days));
                }

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    distributions.Add(new LogDistribution
                    {
                        TimePoint = reader.GetString(0),
                        TotalCount = reader.GetInt32(1),
                        ErrorCount = reader.GetInt32(2),
                        InfoCount = reader.GetInt32(3),
                        WarningCount = reader.GetInt32(4)
                    });
                }
            }

            return Ok(ApiResponse<List<LogDistribution>>.Success(distributions, "获取日志分布数据成功"));
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "获取日志分布数据时发生错误，时间范围：{TimeRange}", timeRange ?? "null");
            }

            return Ok(ApiResponse<List<LogDistribution>>.Fail(ErrorCode.InternalServerError,
                $"获取日志分布数据时发生错误: {ex.Message}"));
        }
    }
}

/// <summary>
/// 日志统计结果模型
/// </summary>
[Serializable]
public class LogStatistics
{
    /// <summary>
    /// 总日志数量
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 各日志级别的数量统计
    /// </summary>
    public Dictionary<string, int> LevelCounts { get; set; } = new Dictionary<string, int>();
}

/// <summary>
/// 分页响应模型，包含分页信息和数据
/// </summary>
[Serializable]
public class PaginatedResponse<T>
{
    /// <summary>
    /// 数据列表
    /// </summary>
    public List<T> Data { get; set; } = [];

    /// <summary>
    /// 总记录数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 当前页码
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>
    /// 每页记录数
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages { get; set; }
}

/// <summary>
/// 日志分布数据模型，用于表示按时间分布的日志数量
/// </summary>
[Serializable]
public class LogDistribution
{
    /// <summary>
    /// 时间点，格式为"HH:00"或"YYYY-MM-DD"
    /// </summary>
    public string TimePoint { get; set; } = "";

    /// <summary>
    /// 该时间点的日志总数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 该时间点的错误日志数量
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// 该时间点的信息日志数量
    /// </summary>
    public int InfoCount { get; set; }

    /// <summary>
    /// 该时间点的警告日志数量
    /// </summary>
    public int WarningCount { get; set; }
}

/// <summary>
/// 日志条目模型，用于表示单条日志记录
/// </summary>
[Serializable]
public class LogEntry
{
    /// <summary>
    /// 日志时间戳
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 日志级别（如 Information, Warning, Error等）
    /// </summary>
    public string Level { get; set; } = "";

    /// <summary>
    /// 异常信息（如果有的话）
    /// </summary>
    public string? Exception { get; set; }

    /// <summary>
    /// 日志属性字典
    /// </summary>
    public Dictionary<string, object>? Properties { get; set; }

    /// <summary>
    /// 日志消息内容
    /// </summary>
    public string? Message { get; set; }
}