using System.Text;
using MarketOurs.WebAPI.Services;

namespace MarketOurs.WebAPI.Middlewares;

/// <summary>
/// 数据脱敏中间件
/// </summary>
public class DataMaskingMiddleware(
    RequestDelegate next,
    ILogger<DataMaskingMiddleware> logger,
    DataMaskingService maskingService)
{
    /// <summary>
    /// 中间件执行方法
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // 检查是否需要脱敏处理
        if (!ShouldMask(context))
        {
            await next(context);
            return;
        }

        // 保存原始响应流
        var originalResponseStream = context.Response.Body;

        try
        {
            // 使用内存流替换原始响应流
            using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            // 执行后续中间件
            await next(context);

            // 重置内存流位置
            memoryStream.Seek(0, SeekOrigin.Begin);

            // 读取响应内容
            var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();

            // 对响应内容进行脱敏处理
            var maskedResponseBody = await MaskResponseBody(responseBody);

            // 重置内存流位置并写入脱敏后的内容
            memoryStream.Seek(0, SeekOrigin.Begin);
            await memoryStream.WriteAsync(Encoding.UTF8.GetBytes(maskedResponseBody));

            // 重置内存流位置，以便原始响应流读取
            memoryStream.Seek(0, SeekOrigin.Begin);

            // 将脱敏后的内容复制到原始响应流
            await memoryStream.CopyToAsync(originalResponseStream);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "数据脱敏处理失败");
            // 恢复原始响应流
            context.Response.Body = originalResponseStream;
        }
        finally
        {
            // 确保恢复原始响应流
            context.Response.Body = originalResponseStream;
        }
    }

    /// <summary>
    /// 判断是否需要进行脱敏处理
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>是否需要脱敏</returns>
    private bool ShouldMask(HttpContext context)
    {
        // 仅对API响应进行脱敏处理
        if (!context.Request.Path.StartsWithSegments("/api"))
            return false;

        // 仅对JSON响应进行脱敏处理
        var contentType = context.Response.Headers.ContentType.ToString();
        if (!contentType.Contains("application/json"))
            return false;

        // 可以根据需要添加更多条件，例如排除某些特定端点
        var excludedPaths = new List<string> { "/api/health", "/api/metrics" };
        return !excludedPaths.Any(path => context.Request.Path.StartsWithSegments(path));
    }

    /// <summary>
    /// 对响应体进行脱敏处理
    /// </summary>
    /// <param name="responseBody">响应体内容</param>
    /// <returns>脱敏后的响应体</returns>
    private Task<string> MaskResponseBody(string responseBody)
    {
        if (string.IsNullOrEmpty(responseBody))
            return Task.FromResult(responseBody);

        try
        {
            // 使用数据脱敏服务对JSON响应进行脱敏处理
            return Task.FromResult(maskingService.MaskJson(responseBody));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "响应体脱敏处理失败，响应内容: {ResponseBody}",
                responseBody.Substring(0, Math.Min(500, responseBody.Length)));
            return Task.FromResult(responseBody);
        }
    }
}