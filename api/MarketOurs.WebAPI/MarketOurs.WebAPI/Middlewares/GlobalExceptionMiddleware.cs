using System.Net;
using System.Text.Json;
using MarketOurs.DataAPI.Exceptions;
using MarketOurs.WebAPI.Controllers;
using Microsoft.IdentityModel.Tokens;

namespace MarketOurs.WebAPI.Middlewares;

/// <summary>
/// 全局异常处理中间件
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="next">下一个中间件</param>
    /// <param name="logger">日志记录器</param>
    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// 中间件执行方法
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // 生成或获取请求ID
        var requestId = context.TraceIdentifier;

        try
        {
            // 将请求ID添加到响应头，方便客户端追踪
            context.Response.Headers.Append("X-Request-ID", requestId);
            await _next(context);
        }
        catch (Exception ex)
        {
            // 记录请求上下文信息
            var requestInfo = new
            {
                context.Request.Method,
                context.Request.Path,
                QueryString = context.Request.QueryString.ToString(),
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                RemoteIp = context.Connection.RemoteIpAddress?.ToString(),
                RequestId = requestId,
                CorrelationId = context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId)
                    ? correlationId.ToString()
                    : "N/A",
                context.Request.ContentType,
                context.Request.ContentLength
            };

            LogException(ex, requestInfo);

            await HandleExceptionAsync(context, ex, requestId);
        }
    }

    private void LogException(Exception exception, object requestInfo)
    {
        var (level, statusCode, errorCode) = GetLogMetadata(exception);

        var message =
            "Request failed. StatusCode: {StatusCode}, ErrorCode: {ErrorCode}, ExceptionType: {ExceptionType}, Request: {@RequestInfo}";

        switch (level)
        {
            case LogLevel.Information:
                _logger.LogInformation(message, statusCode, errorCode, exception.GetType().Name, requestInfo);
                break;
            case LogLevel.Warning:
                _logger.LogWarning(exception, message, statusCode, errorCode, exception.GetType().Name, requestInfo);
                break;
            default:
                _logger.LogError(exception, message, statusCode, errorCode, exception.GetType().Name, requestInfo);
                break;
        }
    }

    private static (LogLevel level, int statusCode, int errorCode) GetLogMetadata(Exception exception)
    {
        var statusCode = exception switch
        {
            CustomException customException => customException.HttpStatusCode,
            ArgumentNullException => (int)HttpStatusCode.BadRequest,
            ArgumentException => (int)HttpStatusCode.BadRequest,
            InvalidOperationException => (int)HttpStatusCode.BadRequest,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            System.Security.Authentication.AuthenticationException => (int)HttpStatusCode.Unauthorized,
            SecurityTokenExpiredException => (int)HttpStatusCode.Unauthorized,
            FluentValidation.ValidationException => (int)HttpStatusCode.BadRequest,
            Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => (int)HttpStatusCode.Conflict,
            Microsoft.EntityFrameworkCore.DbUpdateException => (int)HttpStatusCode.BadRequest,
            HttpRequestException => (int)HttpStatusCode.ServiceUnavailable,
            TimeoutException => (int)HttpStatusCode.RequestTimeout,
            OperationCanceledException => (int)HttpStatusCode.ServiceUnavailable,
            _ => (int)HttpStatusCode.InternalServerError
        };

        var errorCode = exception switch
        {
            CustomException customException => customException.ErrorCode,
            ArgumentNullException => ErrorCode.ParameterEmpty,
            ArgumentException => ErrorCode.ParameterFormatError,
            InvalidOperationException => ErrorCode.InvalidStatusForOperation,
            KeyNotFoundException => ErrorCode.ResourceNotFound,
            UnauthorizedAccessException => ErrorCode.Unauthorized,
            System.Security.Authentication.AuthenticationException => ErrorCode.InvalidToken,
            SecurityTokenExpiredException => ErrorCode.TokenExpired,
            FluentValidation.ValidationException => ErrorCode.ParameterValidationFailed,
            Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => ErrorCode.DataProcessingFailed,
            Microsoft.EntityFrameworkCore.DbUpdateException => ErrorCode.DataProcessingFailed,
            HttpRequestException => ErrorCode.NetworkError,
            TimeoutException => ErrorCode.ExternalServiceTimeout,
            OperationCanceledException => ErrorCode.OperationFailed,
            _ => ErrorCode.InternalServerError
        };

        var level = statusCode switch
        {
            401 or 403 or 404 => LogLevel.Information,
            400 or 409 or 429 => LogLevel.Warning,
            _ when statusCode >= 500 => LogLevel.Error,
            _ => LogLevel.Warning
        };

        return (level, statusCode, errorCode);
    }

    /// <summary>
    /// 处理异常并返回标准化响应
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="exception">异常对象</param>
    /// <param name="requestId">请求ID</param>
    private static async Task HandleExceptionAsync(HttpContext context, Exception exception, string requestId)
    {
        context.Response.ContentType = "application/json";
        var env = context.RequestServices?.GetService<IHostEnvironment>();

        var response = new ApiResponse<object>
        {
            Code = (int)HttpStatusCode.InternalServerError,
            ErrorCode = ErrorCode.InternalServerError,
            Message = "服务器内部错误",
            Data = null,
            RequestId = requestId
        };

        // 根据不同异常类型设置不同的错误码和消息
        switch (exception)
        {
            // 处理数据访问异常
            case DataAccessException dataAccessEx:
                response.Code = dataAccessEx.HttpStatusCode;
                response.ErrorCode = dataAccessEx.ErrorCode;
                response.Message = dataAccessEx.Message;
                response.Detail = dataAccessEx.Detail;
                break;

            // 处理自定义验证异常
            case ValidationException validationException:
                response.Code = validationException.HttpStatusCode;
                response.ErrorCode = validationException.ErrorCode;
                response.Message = validationException.Message;
                if (validationException.ValidationErrors != null && validationException.ValidationErrors.Any())
                {
                    response.Detail = string.Join(", ",
                        validationException.ValidationErrors.SelectMany(e =>
                            e.Value.Select(error => $"{e.Key}: {error}")));
                }
                else
                {
                    response.Detail = validationException.Detail;
                }

                break;

            // 处理访问权限不足异常
            case ResourceAccessException accessEx:
                response.Code = accessEx.HttpStatusCode;
                response.ErrorCode = accessEx.ErrorCode;
                response.Message = accessEx.Message;
                response.Detail = accessEx.Detail;
                break;

            // 处理业务异常
            case BusinessException businessEx:
                response.Code = businessEx.HttpStatusCode;
                response.ErrorCode = businessEx.ErrorCode;
                response.Message = businessEx.Message;
                response.Detail = businessEx.Detail;
                break;

            // 处理身份认证异常
            case AuthException authEx:
                response.Code = authEx.HttpStatusCode;
                response.ErrorCode = authEx.ErrorCode;
                response.Message = authEx.Message;
                response.Detail = authEx.Detail;
                break;

            // 处理其他自定义异常
            case CustomException customException:
                response.Code = customException.HttpStatusCode;
                response.ErrorCode = customException.ErrorCode;
                response.Message = customException.Message;
                response.Detail = customException.Detail;
                break;

            // 处理参数异常
            case ArgumentNullException argNullException:
                response.Code = (int)HttpStatusCode.BadRequest;
                response.ErrorCode = ErrorCode.ParameterEmpty;
                response.Message = $"请求参数不能为空: {argNullException.ParamName}";
                break;

            case ArgumentException argException:
                response.Code = (int)HttpStatusCode.BadRequest;
                response.ErrorCode = ErrorCode.ParameterFormatError;
                response.Message = string.IsNullOrEmpty(argException.ParamName)
                    ? argException.Message
                    : $"请求参数格式错误: {argException.ParamName} - {argException.Message}";
                break;

            // 处理业务逻辑异常
            case InvalidOperationException invalidOpException:
                response.Code = (int)HttpStatusCode.BadRequest;
                response.ErrorCode = ErrorCode.InvalidStatusForOperation;
                response.Message = invalidOpException.Message;
                break;

            // 处理资源访问异常
            case KeyNotFoundException:
                response.Code = (int)HttpStatusCode.NotFound;
                response.ErrorCode = ErrorCode.ResourceNotFound;
                response.Message = "请求的资源不存在";
                break;

            // 处理认证授权异常
            case UnauthorizedAccessException:
                response.Code = (int)HttpStatusCode.Unauthorized;
                response.ErrorCode = ErrorCode.Unauthorized;
                response.Message = "未授权访问";
                break;

            case System.Security.Authentication.AuthenticationException authException:
                response.Code = (int)HttpStatusCode.Unauthorized;
                response.ErrorCode = ErrorCode.InvalidToken;
                response.Message = authException.Message;
                break;

            case SecurityTokenExpiredException:
                response.Code = (int)HttpStatusCode.Unauthorized;
                response.ErrorCode = ErrorCode.TokenExpired;
                response.Message = "访问令牌已过期";
                break;

            // 处理验证异常
            case FluentValidation.ValidationException validationException:
                response.Code = (int)HttpStatusCode.BadRequest;
                response.ErrorCode = ErrorCode.ParameterValidationFailed;
                response.Message = "请求参数验证失败";
                response.Detail = string.Join(", ",
                    validationException.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
                break;

            // 处理数据库异常
            case Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException:
                response.Code = (int)HttpStatusCode.Conflict;
                response.ErrorCode = ErrorCode.DataProcessingFailed;
                response.Message = "数据并发冲突，同一资源被同时修改";
                break;

            case Microsoft.EntityFrameworkCore.DbUpdateException dbUpdateEx:
                response.Code = (int)HttpStatusCode.BadRequest;
                response.ErrorCode = ErrorCode.DataProcessingFailed;
                response.Message = "数据更新失败";
                if (env?.IsDevelopment() ?? false)
                {
                    response.Detail = dbUpdateEx.Message;
                }

                break;

            // 处理其他异常
            case HttpRequestException httpEx:
                response.Code = (int)HttpStatusCode.ServiceUnavailable;
                response.ErrorCode = ErrorCode.NetworkError;
                response.Message = "网络请求失败";
                if (env?.IsDevelopment() ?? false)
                {
                    response.Detail = httpEx.Message;
                }

                break;

            case TimeoutException timeoutEx:
                response.Code = (int)HttpStatusCode.RequestTimeout;
                response.ErrorCode = ErrorCode.ExternalServiceTimeout;
                response.Message = "操作超时";
                if (env?.IsDevelopment() ?? false)
                {
                    response.Detail = timeoutEx.Message;
                }

                break;

            case OperationCanceledException canceledEx:
                response.Code = (int)HttpStatusCode.ServiceUnavailable;
                response.ErrorCode = ErrorCode.OperationFailed;
                response.Message = "操作被取消";
                if (env?.IsDevelopment() ?? false)
                {
                    response.Detail = canceledEx.Message;
                }

                break;

            // 处理其他异常
            default:
                response.Message = exception.Message;
                if (env?.IsDevelopment() ?? false)
                {
                    response.Detail = exception.StackTrace;
                }

                break;
        }

        // 添加请求ID到响应头
        context.Response.Headers.Append("X-Request-ID", requestId);

        // 设置请求ID和时间戳
        response.RequestId = requestId;
        response.Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        context.Response.StatusCode = response.Code;
        await context.Response.WriteAsync(jsonResponse);
    }
}
