using System.Net;
using System.Text.Json;
using MarketOurs.DataAPI.Exceptions;
using MarketOurs.WebAPI.Controllers;
using Microsoft.IdentityModel.Tokens;

namespace MarketOurs.WebAPI.Middlewares;

/// <summary>
/// 全局异常处理中间件。
/// 将所有未捕获异常转换为统一的 ApiResponse 格式，包含 errorCode + errorName。
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.TraceIdentifier;

        try
        {
            context.Response.Headers.Append("X-Request-ID", requestId);
            await _next(context);
        }
        catch (Exception ex)
        {
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
            KeyNotFoundException => ErrorCode.PostNotFound,
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
    private static async Task HandleExceptionAsync(HttpContext context, Exception exception, string requestId)
    {
        context.Response.ContentType = "application/json";
        var env = context.RequestServices?.GetService<IHostEnvironment>();

        // 默认：500 内部错误
        int httpStatus = (int)HttpStatusCode.InternalServerError;
        int errorCode = ErrorCode.InternalServerError;
        string message = "服务器内部错误";
        string? detail = null;

        switch (exception)
        {
            case DataAccessException dataAccessEx:
                httpStatus = dataAccessEx.HttpStatusCode;
                errorCode = dataAccessEx.ErrorCode;
                message = dataAccessEx.Message;
                detail = dataAccessEx.Detail;
                break;

            case ValidationException validationException:
                httpStatus = validationException.HttpStatusCode;
                errorCode = validationException.ErrorCode;
                message = validationException.Message;
                if (validationException.ValidationErrors != null && validationException.ValidationErrors.Any())
                {
                    detail = string.Join(", ",
                        validationException.ValidationErrors.SelectMany(e =>
                            e.Value.Select(error => $"{e.Key}: {error}")));
                }
                else
                {
                    detail = validationException.Detail;
                }
                break;

            case ResourceAccessException accessEx:
                httpStatus = accessEx.HttpStatusCode;
                errorCode = accessEx.ErrorCode;
                message = accessEx.Message;
                detail = accessEx.Detail;
                break;

            case BusinessException businessEx:
                httpStatus = businessEx.HttpStatusCode;
                errorCode = businessEx.ErrorCode;
                message = businessEx.Message;
                detail = businessEx.Detail;
                break;

            case AuthException authEx:
                httpStatus = authEx.HttpStatusCode;
                errorCode = authEx.ErrorCode;
                message = authEx.Message;
                detail = authEx.Detail;
                break;

            case CustomException customException:
                httpStatus = customException.HttpStatusCode;
                errorCode = customException.ErrorCode;
                message = customException.Message;
                detail = customException.Detail;
                break;

            case ArgumentNullException argNullException:
                httpStatus = (int)HttpStatusCode.BadRequest;
                errorCode = ErrorCode.ParameterEmpty;
                message = $"请求参数不能为空: {argNullException.ParamName}";
                break;

            case ArgumentException argException:
                httpStatus = (int)HttpStatusCode.BadRequest;
                errorCode = ErrorCode.ParameterFormatError;
                message = string.IsNullOrEmpty(argException.ParamName)
                    ? argException.Message
                    : $"请求参数格式错误: {argException.ParamName} - {argException.Message}";
                break;

            case InvalidOperationException invalidOpException:
                httpStatus = (int)HttpStatusCode.BadRequest;
                errorCode = ErrorCode.InvalidStatusForOperation;
                message = invalidOpException.Message;
                break;

            case KeyNotFoundException:
                httpStatus = (int)HttpStatusCode.NotFound;
                errorCode = ErrorCode.PostNotFound;
                message = "请求的资源不存在";
                break;

            case UnauthorizedAccessException:
                httpStatus = (int)HttpStatusCode.Unauthorized;
                errorCode = ErrorCode.Unauthorized;
                message = "未授权访问";
                break;

            case System.Security.Authentication.AuthenticationException authException:
                httpStatus = (int)HttpStatusCode.Unauthorized;
                errorCode = ErrorCode.InvalidToken;
                message = authException.Message;
                break;

            case SecurityTokenExpiredException:
                httpStatus = (int)HttpStatusCode.Unauthorized;
                errorCode = ErrorCode.TokenExpired;
                message = "访问令牌已过期";
                break;

            case FluentValidation.ValidationException validationException:
                httpStatus = (int)HttpStatusCode.BadRequest;
                errorCode = ErrorCode.ParameterValidationFailed;
                message = "请求参数验证失败";
                detail = string.Join(", ",
                    validationException.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
                break;

            case Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException:
                httpStatus = (int)HttpStatusCode.Conflict;
                errorCode = ErrorCode.DataProcessingFailed;
                message = "数据并发冲突，同一资源被同时修改";
                break;

            case Microsoft.EntityFrameworkCore.DbUpdateException dbUpdateEx:
                httpStatus = (int)HttpStatusCode.BadRequest;
                errorCode = ErrorCode.DataProcessingFailed;
                message = "数据更新失败";
                if (env?.IsDevelopment() ?? false)
                {
                    detail = dbUpdateEx.Message;
                }
                break;

            case HttpRequestException httpEx:
                httpStatus = (int)HttpStatusCode.ServiceUnavailable;
                errorCode = ErrorCode.NetworkError;
                message = "网络请求失败";
                if (env?.IsDevelopment() ?? false)
                {
                    detail = httpEx.Message;
                }
                break;

            case TimeoutException timeoutEx:
                httpStatus = (int)HttpStatusCode.RequestTimeout;
                errorCode = ErrorCode.ExternalServiceTimeout;
                message = "操作超时";
                if (env?.IsDevelopment() ?? false)
                {
                    detail = timeoutEx.Message;
                }
                break;

            case OperationCanceledException canceledEx:
                httpStatus = (int)HttpStatusCode.ServiceUnavailable;
                errorCode = ErrorCode.OperationFailed;
                message = "操作被取消";
                if (env?.IsDevelopment() ?? false)
                {
                    detail = canceledEx.Message;
                }
                break;

            default:
                // 未知异常：在生产环境隐藏详细信息
                message = exception.Message;
                if (env?.IsDevelopment() ?? false)
                {
                    detail = exception.StackTrace;
                }
                break;
        }

        var response = ApiResponse<object>.FailWithExplicitMessage(
            httpStatus, errorCode, message, detail, requestId);

        context.Response.Headers.Append("X-Request-ID", requestId);
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
