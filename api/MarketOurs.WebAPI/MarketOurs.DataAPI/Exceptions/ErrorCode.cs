using System.Net;

namespace MarketOurs.DataAPI.Exceptions;

/// <summary>
/// 项目级业务错误码定义。
/// </summary>
public static class ErrorCode
{
    public const int Success = 0;

    #region 参数错误 (1000-1099)
    public const int ParameterEmpty = 1000;
    public const int ParameterFormatError = 1001;
    public const int ParameterOutOfRange = 1002;
    public const int ParameterValidationFailed = 1003;
    #endregion

    #region 通用业务错误 (2000-2099)
    public const int ResourceAlreadyExists = 2000;
    public const int OperationFailed = 2001;
    public const int DataProcessingFailed = 2002;
    public const int InvalidStatusForOperation = 2003;
    #endregion

    #region 认证与权限错误 (3000-3099)
    public const int Unauthorized = 3000;
    public const int InsufficientPermission = 3001;
    public const int LoginExpired = 3002;
    public const int InvalidToken = 3003;
    public const int TokenExpired = 3004;
    public const int OAuthProviderNotSupported = 3005;
    #endregion

    #region 通用资源错误 (4000-4099)
    public const int ResourceNotFound = 4000;
    #endregion

    #region 用户错误 (4100-4199)
    public const int UserNotFound = 4100;
    public const int UserNotActive = 4101;
    public const int AccountAlreadyExists = 4102;
    public const int PasswordMismatch = 4103;
    #endregion

    #region 帖子错误 (4200-4299)
    public const int PostNotFound = 4200;
    public const int PostCreateFailed = 4201;
    public const int PostUpdateFailed = 4202;
    public const int PostDeleteDenied = 4203;
    #endregion

    #region 评论错误 (4300-4399)
    public const int CommentNotFound = 4300;
    public const int ParentCommentNotFound = 4301;
    public const int CommentCreateFailed = 4302;
    public const int CommentUpdateFailed = 4303;
    public const int CommentDeleteDenied = 4304;
    #endregion

    #region 系统错误 (5000-5099)
    public const int InternalServerError = 5000;
    public const int DatabaseOperationFailed = 5001;
    public const int CacheOperationFailed = 5002;
    public const int NetworkError = 5003;
    #endregion

    #region 外部服务错误 (6000-6099)
    public const int ExternalServiceFailed = 6000;
    public const int ExternalServiceTimeout = 6001;
    public const int ExternalServiceReturnError = 6002;
    public const int ExternalServiceNotConfigured = 6003;
    #endregion

    #region 平台错误 (7000-7099)
    public const int TooManyRequests = 7000;
    public const int InvalidRequest = 7001;
    #endregion

    /// <summary>
    /// 将业务错误码映射到默认 HTTP 状态码。
    /// </summary>
    public static int ToHttpStatusCode(int errorCode)
    {
        return errorCode switch
        {
            Success => (int)HttpStatusCode.OK,

            ParameterEmpty or
            ParameterFormatError or
            ParameterOutOfRange or
            ParameterValidationFailed or
            InvalidRequest or
            OperationFailed or
            DataProcessingFailed or
            InvalidStatusForOperation or
            PasswordMismatch or
            PostCreateFailed or
            PostUpdateFailed or
            CommentCreateFailed or
            CommentUpdateFailed => (int)HttpStatusCode.BadRequest,

            ResourceAlreadyExists or
            AccountAlreadyExists => (int)HttpStatusCode.Conflict,

            Unauthorized or
            LoginExpired or
            InvalidToken or
            TokenExpired => (int)HttpStatusCode.Unauthorized,

            InsufficientPermission or
            UserNotActive or
            PostDeleteDenied or
            CommentDeleteDenied => (int)HttpStatusCode.Forbidden,

            ResourceNotFound or
            UserNotFound or
            PostNotFound or
            CommentNotFound or
            ParentCommentNotFound => (int)HttpStatusCode.NotFound,

            TooManyRequests => (int)HttpStatusCode.TooManyRequests,

            NetworkError => (int)HttpStatusCode.ServiceUnavailable,

            ExternalServiceTimeout => (int)HttpStatusCode.RequestTimeout,

            DatabaseOperationFailed or
            CacheOperationFailed or
            InternalServerError or
            ExternalServiceFailed or
            ExternalServiceReturnError or
            ExternalServiceNotConfigured => (int)HttpStatusCode.InternalServerError,

            OAuthProviderNotSupported => (int)HttpStatusCode.BadRequest,

            _ => (int)HttpStatusCode.BadRequest
        };
    }
}
