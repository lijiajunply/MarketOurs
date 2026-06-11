namespace MarketOurs.WebAPI.Controllers;

/// <summary>
/// 统一API响应模型。
/// 错误响应通过 errorCode + errorName 即可唯一标识错误，message 仅作为辅助描述。
/// </summary>
/// <typeparam name="T">响应数据类型</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// HTTP 状态码（200表示成功）
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// 业务错误码（0表示成功，其他表示具体错误类型）。
    /// 客户端应基于此字段做程序化错误处理，而非解析 message。
    /// </summary>
    public int ErrorCode { get; set; }

    /// <summary>
    /// 错误码名称（如 "PostNotFound"），方便调试和日志检索。
    /// </summary>
    public string ErrorName { get; set; } = string.Empty;

    /// <summary>
    /// 响应消息（成功时为 "Success"；失败时为人类可读描述，仅作辅助）。
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 详细描述（开发环境可能包含堆栈等信息）。
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// 响应数据
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// 请求ID，用于追踪请求
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// 响应时间戳（UTC时间）
    /// </summary>
    public string? Timestamp { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public ApiResponse()
    {
        Code = 200;
        ErrorCode = 0;
        ErrorName = DataAPI.Exceptions.ErrorCode.GetName(0);
        Message = "Success";
        Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
    }

    /// <summary>
    /// 成功响应
    /// </summary>
    public static ApiResponse<T> Success(T data, string message = "Success", string? detail = null, string? requestId = null)
    {
        return new ApiResponse<T>
        {
            Code = 200,
            ErrorCode = DataAPI.Exceptions.ErrorCode.Success,
            ErrorName = DataAPI.Exceptions.ErrorCode.GetName(DataAPI.Exceptions.ErrorCode.Success),
            Message = message,
            Detail = detail,
            Data = data,
            RequestId = requestId,
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };
    }

    /// <summary>
    /// 失败响应（使用 errorCode 自动推导 HTTP 状态码）。
    /// </summary>
    /// <param name="errorCode">业务错误码</param>
    /// <param name="message">错误消息（可选，辅助描述）</param>
    /// <param name="detail">详细描述</param>
    /// <param name="requestId">请求ID</param>
    public static ApiResponse<T> Fail(int errorCode, string message = "", string? detail = null, string? requestId = null)
    {
        return new ApiResponse<T>
        {
            Code = GetHttpStatusCodeFromErrorCode(errorCode),
            ErrorCode = errorCode,
            ErrorName = DataAPI.Exceptions.ErrorCode.GetName(errorCode),
            Message = string.IsNullOrEmpty(message) ? GetDefaultMessage(errorCode) : message,
            Detail = detail,
            RequestId = requestId,
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };
    }

    /// <summary>
    /// 失败响应（显式指定 HTTP 状态码）。
    /// </summary>
    public static ApiResponse<T> Fail(int httpStatusCode, int errorCode, string message = "", string? detail = null, string? requestId = null)
    {
        return new ApiResponse<T>
        {
            Code = httpStatusCode,
            ErrorCode = errorCode,
            ErrorName = DataAPI.Exceptions.ErrorCode.GetName(errorCode),
            Message = string.IsNullOrEmpty(message) ? GetDefaultMessage(errorCode) : message,
            Detail = detail,
            RequestId = requestId,
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };
    }

    /// <summary>
    /// 失败响应（使用原始消息，不自动补全 — 用于中间件从异常构造响应）。
    /// </summary>
    internal static ApiResponse<T> FailWithExplicitMessage(int httpStatusCode, int errorCode, string message, string? detail = null, string? requestId = null)
    {
        return new ApiResponse<T>
        {
            Code = httpStatusCode,
            ErrorCode = errorCode,
            ErrorName = DataAPI.Exceptions.ErrorCode.GetName(errorCode),
            Message = message,
            Detail = detail,
            RequestId = requestId,
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };
    }

    /// <summary>
    /// 根据业务错误码获取对应的HTTP状态码
    /// </summary>
    protected static int GetHttpStatusCodeFromErrorCode(int errorCode)
    {
        return DataAPI.Exceptions.ErrorCode.ToHttpStatusCode(errorCode);
    }

    /// <summary>
    /// 获取错误码对应的默认消息（用于 message 为空时的自动填充）。
    /// 注意：这只是兜底，客户端应基于 errorCode 做判断。
    /// </summary>
    protected static string GetDefaultMessage(int errorCode)
    {
        return errorCode switch
        {
            DataAPI.Exceptions.ErrorCode.Success => "Success",

            DataAPI.Exceptions.ErrorCode.ParameterEmpty => "请求参数不能为空",
            DataAPI.Exceptions.ErrorCode.ParameterFormatError => "请求参数格式错误",
            DataAPI.Exceptions.ErrorCode.ParameterOutOfRange => "请求参数超出范围",
            DataAPI.Exceptions.ErrorCode.ParameterValidationFailed => "请求参数验证失败",
            DataAPI.Exceptions.ErrorCode.RequestBodyMissing => "请求体缺失",
            DataAPI.Exceptions.ErrorCode.UnsupportedContentType => "不支持的 Content-Type",

            DataAPI.Exceptions.ErrorCode.ResourceAlreadyExists => "资源已存在",
            DataAPI.Exceptions.ErrorCode.OperationFailed => "操作失败",
            DataAPI.Exceptions.ErrorCode.DataProcessingFailed => "数据处理失败",
            DataAPI.Exceptions.ErrorCode.InvalidStatusForOperation => "当前状态不允许此操作",
            DataAPI.Exceptions.ErrorCode.OperationTooFrequent => "操作过于频繁，请稍后再试",
            DataAPI.Exceptions.ErrorCode.ResourceExpired => "资源已过期",

            DataAPI.Exceptions.ErrorCode.Unauthorized => "未认证，请先登录",
            DataAPI.Exceptions.ErrorCode.InvalidToken => "令牌无效",
            DataAPI.Exceptions.ErrorCode.TokenExpired => "令牌已过期",
            DataAPI.Exceptions.ErrorCode.LoginExpired => "登录已过期，请重新登录",
            DataAPI.Exceptions.ErrorCode.RefreshTokenInvalid => "刷新令牌无效或已过期",
            DataAPI.Exceptions.ErrorCode.InvalidCredentials => "用户名或密码错误",
            DataAPI.Exceptions.ErrorCode.OAuthCodeInvalid => "OAuth 授权码无效",
            DataAPI.Exceptions.ErrorCode.OAuthProviderNotSupported => "不支持的第三方登录方式",
            DataAPI.Exceptions.ErrorCode.VerificationCodeInvalid => "验证码无效或已过期",
            DataAPI.Exceptions.ErrorCode.RegistrationSessionExpired => "注册会话已过期，请重新开始",
            DataAPI.Exceptions.ErrorCode.OAuthAccountNotLinked => "第三方账号未绑定本地账户",
            DataAPI.Exceptions.ErrorCode.OAuthAccountAlreadyLinked => "该第三方账号已被其他账户绑定",
            DataAPI.Exceptions.ErrorCode.LinkedAccountNotFound => "未找到关联账户",

            DataAPI.Exceptions.ErrorCode.InsufficientPermission => "权限不足",
            DataAPI.Exceptions.ErrorCode.PostEditDenied => "无权修改他人的帖子",
            DataAPI.Exceptions.ErrorCode.PostDeleteDenied => "无权删除他人的帖子",
            DataAPI.Exceptions.ErrorCode.CommentEditDenied => "无权修改他人的评论",
            DataAPI.Exceptions.ErrorCode.CommentDeleteDenied => "无权删除他人的评论",
            DataAPI.Exceptions.ErrorCode.AccountDisabled => "账号已被禁用",
            DataAPI.Exceptions.ErrorCode.AccountNotActive => "账号尚未激活",

            DataAPI.Exceptions.ErrorCode.UserNotFound => "用户不存在",
            DataAPI.Exceptions.ErrorCode.UserEmailNotBound => "用户未绑定邮箱",
            DataAPI.Exceptions.ErrorCode.UserPhoneNotBound => "用户未绑定手机号",
            DataAPI.Exceptions.ErrorCode.AccountAlreadyExists => "该账号已存在",
            DataAPI.Exceptions.ErrorCode.EmailAlreadyRegistered => "邮箱已被注册",
            DataAPI.Exceptions.ErrorCode.OldPasswordMismatch => "旧密码错误",
            DataAPI.Exceptions.ErrorCode.PasswordMismatch => "密码验证失败",
            DataAPI.Exceptions.ErrorCode.CannotOperateSelf => "不能操作自己的账号",
            DataAPI.Exceptions.ErrorCode.NoRecoveryChannelAvailable => "该账号未绑定邮箱或手机号，无法接收重置验证码",
            DataAPI.Exceptions.ErrorCode.UnsupportedVerificationMethod => "不支持的验证方式",
            DataAPI.Exceptions.ErrorCode.UnsupportedOAuthPlatform => "不支持的第三方平台",
            DataAPI.Exceptions.ErrorCode.OAuthNotBound => "该第三方账号尚未绑定",

            DataAPI.Exceptions.ErrorCode.PostNotFound => "帖子不存在",
            DataAPI.Exceptions.ErrorCode.PostCreateFailed => "帖子创建失败",
            DataAPI.Exceptions.ErrorCode.PostUpdateFailed => "帖子更新失败",

            DataAPI.Exceptions.ErrorCode.CommentNotFound => "评论不存在",
            DataAPI.Exceptions.ErrorCode.ParentCommentNotFound => "要回复的评论不存在",
            DataAPI.Exceptions.ErrorCode.CommentCreateFailed => "评论创建失败",
            DataAPI.Exceptions.ErrorCode.CommentUpdateFailed => "评论更新失败",

            DataAPI.Exceptions.ErrorCode.CannotFollowSelf => "不能关注自己",
            DataAPI.Exceptions.ErrorCode.CannotBlockSelf => "不能屏蔽自己",
            DataAPI.Exceptions.ErrorCode.CannotFollowBlockedUser => "无法关注已屏蔽或屏蔽您的用户",
            DataAPI.Exceptions.ErrorCode.FollowTooFrequent => "关注操作过于频繁，请稍后再试",
            DataAPI.Exceptions.ErrorCode.BlockTooFrequent => "屏蔽操作过于频繁，请稍后再试",

            DataAPI.Exceptions.ErrorCode.FileNotFound => "文件未找到",
            DataAPI.Exceptions.ErrorCode.UnsupportedFileType => "不支持的文件类型",
            DataAPI.Exceptions.ErrorCode.FileUploadFailed => "文件上传失败",
            DataAPI.Exceptions.ErrorCode.FileSizeExceeded => "文件大小超出限制",

            DataAPI.Exceptions.ErrorCode.LikeTooFrequent => "点赞操作过于频繁，请稍后再试",
            DataAPI.Exceptions.ErrorCode.AlreadyLiked => "已点过赞",
            DataAPI.Exceptions.ErrorCode.NotLikedYet => "未点过赞，无法取消",

            DataAPI.Exceptions.ErrorCode.InternalServerError => "服务器内部错误",
            DataAPI.Exceptions.ErrorCode.DatabaseOperationFailed => "数据库操作失败",
            DataAPI.Exceptions.ErrorCode.CacheUnavailable => "缓存服务不可用",
            DataAPI.Exceptions.ErrorCode.CacheOperationFailed => "缓存操作失败",
            DataAPI.Exceptions.ErrorCode.NetworkError => "网络请求失败",

            DataAPI.Exceptions.ErrorCode.ExternalServiceFailed => "外部服务调用失败",
            DataAPI.Exceptions.ErrorCode.ExternalServiceTimeout => "外部服务超时",
            DataAPI.Exceptions.ErrorCode.ExternalServiceReturnError => "外部服务返回错误",
            DataAPI.Exceptions.ErrorCode.ExternalServiceNotConfigured => "外部服务未配置",
            DataAPI.Exceptions.ErrorCode.EmailSendFailed => "邮件发送失败",
            DataAPI.Exceptions.ErrorCode.SmsSendFailed => "短信发送失败",

            DataAPI.Exceptions.ErrorCode.TooManyRequests => "请求频率过高，请稍后再试",
            DataAPI.Exceptions.ErrorCode.IpBlacklisted => "IP 已被加入黑名单",

            _ => "未知错误"
        };
    }
}

/// <summary>
/// 无数据的API响应模型
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    /// <summary>
    /// 成功响应
    /// </summary>
    public static ApiResponse Success(string message = "Success", string? detail = null, string? requestId = null)
    {
        return new ApiResponse
        {
            Code = 200,
            ErrorCode = DataAPI.Exceptions.ErrorCode.Success,
            ErrorName = DataAPI.Exceptions.ErrorCode.GetName(DataAPI.Exceptions.ErrorCode.Success),
            Message = message,
            Detail = detail,
            RequestId = requestId,
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };
    }

    /// <summary>
    /// 失败响应（使用 errorCode 自动推导 HTTP 状态码）。
    /// </summary>
    public static ApiResponse Fail(int errorCode, string message = "", string? detail = null, string? requestId = null)
    {
        return new ApiResponse
        {
            Code = GetHttpStatusCodeFromErrorCode(errorCode),
            ErrorCode = errorCode,
            ErrorName = DataAPI.Exceptions.ErrorCode.GetName(errorCode),
            Message = string.IsNullOrEmpty(message) ? GetDefaultMessage(errorCode) : message,
            Detail = detail,
            RequestId = requestId,
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };
    }

    /// <summary>
    /// 失败响应（显式指定 HTTP 状态码）。
    /// </summary>
    public static ApiResponse Fail(int httpStatusCode, int errorCode, string message = "", string? detail = null, string? requestId = null)
    {
        return new ApiResponse
        {
            Code = httpStatusCode,
            ErrorCode = errorCode,
            ErrorName = DataAPI.Exceptions.ErrorCode.GetName(errorCode),
            Message = string.IsNullOrEmpty(message) ? GetDefaultMessage(errorCode) : message,
            Detail = detail,
            RequestId = requestId,
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };
    }
}
