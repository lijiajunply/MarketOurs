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
        // 仅拦截API路径（排除特定端点），ContentType在Next执行后才确定，延迟检查
        if (!context.Request.Path.StartsWithSegments("/api") || IsExcludedPath(context))
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

            // Next执行后才能拿到ContentType，非JSON直接透传
            var contentType = context.Response.ContentType ?? "";
            if (!contentType.Contains("application/json"))
            {
                memoryStream.Seek(0, SeekOrigin.Begin);
                await memoryStream.CopyToAsync(originalResponseStream);
                return;
            }

            // 重置内存流位置
            memoryStream.Seek(0, SeekOrigin.Begin);

            // 读取响应内容
            var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();

            // 对响应内容进行脱敏处理
            var maskedResponseBody = await MaskResponseBody(responseBody);

            // 写入脱敏后的内容并复制到原始响应流
            var maskedBytes = Encoding.UTF8.GetBytes(maskedResponseBody);
            await originalResponseStream.WriteAsync(maskedBytes);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "数据脱敏处理失败");
            context.Response.Body = originalResponseStream;
        }
        finally
        {
            // 确保恢复原始响应流
            context.Response.Body = originalResponseStream;
        }
    }

    private bool IsExcludedPath(HttpContext context)
    {
        var excludedPaths = new[] { "/api/health", "/api/metrics" };
        return excludedPaths.Any(path => context.Request.Path.StartsWithSegments(path));
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