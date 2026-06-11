using System.Net;

namespace MarketOurs.DataAPI.Exceptions;

/// <summary>
/// 项目级业务错误码定义。
/// 每个错误场景有唯一错误码，客户端可据此做程序化处理，不再依赖 message 字段做错误判定。
/// </summary>
public static class ErrorCode
{
    public const int Success = 0;

    #region 通用系统错误 (1000-1099)

    /// <summary>请求参数为空</summary>
    public const int ParameterEmpty = 1000;
    /// <summary>请求参数格式错误</summary>
    public const int ParameterFormatError = 1001;
    /// <summary>请求参数超出范围</summary>
    public const int ParameterOutOfRange = 1002;
    /// <summary>请求参数验证失败（FluentValidation）</summary>
    public const int ParameterValidationFailed = 1003;
    /// <summary>请求体缺失</summary>
    public const int RequestBodyMissing = 1004;
    /// <summary>不支持的 Content-Type</summary>
    public const int UnsupportedContentType = 1005;

    #endregion

    #region 通用业务错误 (1100-1199)

    /// <summary>资源已存在（通用）</summary>
    public const int ResourceAlreadyExists = 1100;
    /// <summary>操作失败（通用）</summary>
    public const int OperationFailed = 1101;
    /// <summary>数据处理失败</summary>
    public const int DataProcessingFailed = 1102;
    /// <summary>当前状态不允许此操作</summary>
    public const int InvalidStatusForOperation = 1103;
    /// <summary>操作过于频繁</summary>
    public const int OperationTooFrequent = 1104;
    /// <summary>资源已过期</summary>
    public const int ResourceExpired = 1105;

    #endregion

    #region 认证错误 (2000-2099)

    /// <summary>未认证（未登录）</summary>
    public const int Unauthorized = 2000;
    /// <summary>令牌无效</summary>
    public const int InvalidToken = 2001;
    /// <summary>令牌已过期</summary>
    public const int TokenExpired = 2002;
    /// <summary>登录已过期（需要重新登录）</summary>
    public const int LoginExpired = 2003;
    /// <summary>刷新令牌无效或已过期</summary>
    public const int RefreshTokenInvalid = 2004;
    /// <summary>用户名或密码错误</summary>
    public const int InvalidCredentials = 2005;
    /// <summary>OAuth 授权码无效</summary>
    public const int OAuthCodeInvalid = 2006;
    /// <summary>不支持的 OAuth 提供商</summary>
    public const int OAuthProviderNotSupported = 2007;
    /// <summary>验证码无效或已过期</summary>
    public const int VerificationCodeInvalid = 2008;
    /// <summary>注册会话已过期</summary>
    public const int RegistrationSessionExpired = 2009;
    /// <summary>第三方账号未绑定本地账户</summary>
    public const int OAuthAccountNotLinked = 2010;
    /// <summary>第三方账号已被其他账户绑定</summary>
    public const int OAuthAccountAlreadyLinked = 2011;
    /// <summary>未找到关联账户</summary>
    public const int LinkedAccountNotFound = 2012;

    #endregion

    #region 权限错误 (2100-2199)

    /// <summary>权限不足（通用）</summary>
    public const int InsufficientPermission = 2100;
    /// <summary>无权修改他人的帖子</summary>
    public const int PostEditDenied = 2101;
    /// <summary>无权删除他人的帖子</summary>
    public const int PostDeleteDenied = 2102;
    /// <summary>无权修改他人的评论</summary>
    public const int CommentEditDenied = 2103;
    /// <summary>无权删除他人的评论</summary>
    public const int CommentDeleteDenied = 2104;
    /// <summary>账号已被禁用</summary>
    public const int AccountDisabled = 2105;
    /// <summary>账号尚未激活</summary>
    public const int AccountNotActive = 2106;

    #endregion

    #region 用户错误 (3000-3099)

    /// <summary>用户不存在</summary>
    public const int UserNotFound = 3000;
    /// <summary>用户未绑定邮箱</summary>
    public const int UserEmailNotBound = 3001;
    /// <summary>用户未绑定手机号</summary>
    public const int UserPhoneNotBound = 3002;
    /// <summary>账号已存在（邮箱或手机号已被注册）</summary>
    public const int AccountAlreadyExists = 3003;
    /// <summary>邮箱已被注册</summary>
    public const int EmailAlreadyRegistered = 3004;
    /// <summary>旧密码错误</summary>
    public const int OldPasswordMismatch = 3005;
    /// <summary>密码验证失败</summary>
    public const int PasswordMismatch = 3006;
    /// <summary>不能操作自己的账号</summary>
    public const int CannotOperateSelf = 3007;
    /// <summary>该账号无法接收重置验证码（未绑定邮箱/手机）</summary>
    public const int NoRecoveryChannelAvailable = 3008;
    /// <summary>不支持的验证方式</summary>
    public const int UnsupportedVerificationMethod = 3009;
    /// <summary>不支持的第三方平台</summary>
    public const int UnsupportedOAuthPlatform = 3010;
    /// <summary>该第三方账号尚未绑定</summary>
    public const int OAuthNotBound = 3011;

    #endregion

    #region 帖子错误 (4000-4099)

    /// <summary>帖子不存在</summary>
    public const int PostNotFound = 4000;
    /// <summary>帖子创建失败</summary>
    public const int PostCreateFailed = 4001;
    /// <summary>帖子更新失败</summary>
    public const int PostUpdateFailed = 4002;

    #endregion

    #region 评论错误 (4100-4199)

    /// <summary>评论不存在</summary>
    public const int CommentNotFound = 4100;
    /// <summary>要回复的父评论不存在</summary>
    public const int ParentCommentNotFound = 4101;
    /// <summary>评论创建失败</summary>
    public const int CommentCreateFailed = 4102;
    /// <summary>评论更新失败</summary>
    public const int CommentUpdateFailed = 4103;

    #endregion

    #region 关注/屏蔽错误 (5000-5099)

    /// <summary>不能关注自己</summary>
    public const int CannotFollowSelf = 5000;
    /// <summary>不能屏蔽自己</summary>
    public const int CannotBlockSelf = 5001;
    /// <summary>无法关注已屏蔽或屏蔽您的用户</summary>
    public const int CannotFollowBlockedUser = 5002;
    /// <summary>关注操作过于频繁</summary>
    public const int FollowTooFrequent = 5003;
    /// <summary>屏蔽操作过于频繁</summary>
    public const int BlockTooFrequent = 5004;

    #endregion

    #region 文件错误 (6000-6099)

    /// <summary>文件未找到</summary>
    public const int FileNotFound = 6000;
    /// <summary>不支持的文件类型</summary>
    public const int UnsupportedFileType = 6001;
    /// <summary>文件上传失败</summary>
    public const int FileUploadFailed = 6002;
    /// <summary>文件大小超出限制</summary>
    public const int FileSizeExceeded = 6003;

    #endregion

    #region 点赞错误 (7000-7099)

    /// <summary>点赞操作过于频繁</summary>
    public const int LikeTooFrequent = 7000;
    /// <summary>已点过赞</summary>
    public const int AlreadyLiked = 7001;
    /// <summary>未点过赞，无法取消</summary>
    public const int NotLikedYet = 7002;

    #endregion

    #region 系统/基础设施错误 (8000-8099)

    /// <summary>服务器内部错误</summary>
    public const int InternalServerError = 8000;
    /// <summary>数据库操作失败</summary>
    public const int DatabaseOperationFailed = 8001;
    /// <summary>缓存服务不可用</summary>
    public const int CacheUnavailable = 8002;
    /// <summary>缓存操作失败</summary>
    public const int CacheOperationFailed = 8003;
    /// <summary>网络请求失败</summary>
    public const int NetworkError = 8004;

    #endregion

    #region 外部服务错误 (8100-8199)

    /// <summary>外部服务调用失败（通用）</summary>
    public const int ExternalServiceFailed = 8100;
    /// <summary>外部服务超时</summary>
    public const int ExternalServiceTimeout = 8101;
    /// <summary>外部服务返回错误</summary>
    public const int ExternalServiceReturnError = 8102;
    /// <summary>外部服务未配置</summary>
    public const int ExternalServiceNotConfigured = 8103;
    /// <summary>邮件发送失败</summary>
    public const int EmailSendFailed = 8104;
    /// <summary>短信发送失败</summary>
    public const int SmsSendFailed = 8105;

    #endregion

    #region 平台/限流错误 (9000-9099)

    /// <summary>请求频率过高（触发限流）</summary>
    public const int TooManyRequests = 9000;
    /// <summary>IP 已被加入黑名单</summary>
    public const int IpBlacklisted = 9001;

    #endregion

    /// <summary>
    /// 将业务错误码映射到默认 HTTP 状态码。
    /// </summary>
    public static int ToHttpStatusCode(int errorCode)
    {
        return errorCode switch
        {
            Success => (int)HttpStatusCode.OK,

            // 100x 参数错误 → 400
            ParameterEmpty or
            ParameterFormatError or
            ParameterOutOfRange or
            ParameterValidationFailed or
            RequestBodyMissing or
            UnsupportedContentType => (int)HttpStatusCode.BadRequest,

            // 110x 业务错误
            OperationFailed or
            DataProcessingFailed or
            InvalidStatusForOperation or
            OperationTooFrequent or
            ResourceExpired => (int)HttpStatusCode.BadRequest,

            ResourceAlreadyExists => (int)HttpStatusCode.Conflict,

            // 200x 认证错误 → 401
            Unauthorized or
            InvalidToken or
            TokenExpired or
            LoginExpired or
            RefreshTokenInvalid or
            InvalidCredentials or
            OAuthCodeInvalid or
            VerificationCodeInvalid or
            RegistrationSessionExpired => (int)HttpStatusCode.Unauthorized,

            OAuthProviderNotSupported or
            OAuthAccountNotLinked or
            OAuthAccountAlreadyLinked or
            LinkedAccountNotFound => (int)HttpStatusCode.BadRequest,

            // 210x 权限错误 → 403
            InsufficientPermission => (int)HttpStatusCode.Forbidden,
            PostEditDenied or
            PostDeleteDenied or
            CommentEditDenied or
            CommentDeleteDenied => (int)HttpStatusCode.Forbidden,
            AccountDisabled or
            AccountNotActive => (int)HttpStatusCode.Forbidden,

            // 300x 用户错误
            UserNotFound => (int)HttpStatusCode.NotFound,
            UserEmailNotBound or
            UserPhoneNotBound => (int)HttpStatusCode.BadRequest,
            AccountAlreadyExists or
            EmailAlreadyRegistered => (int)HttpStatusCode.Conflict,
            OldPasswordMismatch or
            PasswordMismatch => (int)HttpStatusCode.BadRequest,
            CannotOperateSelf => (int)HttpStatusCode.Forbidden,
            NoRecoveryChannelAvailable => (int)HttpStatusCode.BadRequest,
            UnsupportedVerificationMethod or
            UnsupportedOAuthPlatform => (int)HttpStatusCode.BadRequest,
            OAuthNotBound => (int)HttpStatusCode.BadRequest,

            // 400x 帖子错误
            PostNotFound => (int)HttpStatusCode.NotFound,
            PostCreateFailed or
            PostUpdateFailed => (int)HttpStatusCode.BadRequest,

            // 410x 评论错误
            CommentNotFound or
            ParentCommentNotFound => (int)HttpStatusCode.NotFound,
            CommentCreateFailed or
            CommentUpdateFailed => (int)HttpStatusCode.BadRequest,

            // 500x 关注/屏蔽
            CannotFollowSelf or
            CannotBlockSelf => (int)HttpStatusCode.BadRequest,
            CannotFollowBlockedUser => (int)HttpStatusCode.Forbidden,
            FollowTooFrequent or
            BlockTooFrequent => (int)HttpStatusCode.TooManyRequests,

            // 600x 文件错误
            FileNotFound => (int)HttpStatusCode.NotFound,
            UnsupportedFileType or
            FileSizeExceeded => (int)HttpStatusCode.BadRequest,
            FileUploadFailed => (int)HttpStatusCode.InternalServerError,

            // 700x 点赞错误
            LikeTooFrequent => (int)HttpStatusCode.TooManyRequests,
            AlreadyLiked => (int)HttpStatusCode.Conflict,
            NotLikedYet => (int)HttpStatusCode.BadRequest,

            // 800x 系统错误 → 500
            InternalServerError or
            DatabaseOperationFailed or
            CacheUnavailable or
            CacheOperationFailed => (int)HttpStatusCode.InternalServerError,

            NetworkError => (int)HttpStatusCode.ServiceUnavailable,

            // 810x 外部服务
            ExternalServiceFailed or
            ExternalServiceReturnError or
            ExternalServiceNotConfigured or
            EmailSendFailed or
            SmsSendFailed => (int)HttpStatusCode.InternalServerError,

            ExternalServiceTimeout => (int)HttpStatusCode.RequestTimeout,

            // 900x 平台/限流
            TooManyRequests or
            IpBlacklisted => (int)HttpStatusCode.TooManyRequests,

            _ => (int)HttpStatusCode.BadRequest
        };
    }

    /// <summary>
    /// 获取错误码的名称（用于 API 响应中的 errorName 字段，方便客户端调试）。
    /// </summary>
    public static string GetName(int errorCode)
    {
        return errorCode switch
        {
            Success => nameof(Success),

            ParameterEmpty => nameof(ParameterEmpty),
            ParameterFormatError => nameof(ParameterFormatError),
            ParameterOutOfRange => nameof(ParameterOutOfRange),
            ParameterValidationFailed => nameof(ParameterValidationFailed),
            RequestBodyMissing => nameof(RequestBodyMissing),
            UnsupportedContentType => nameof(UnsupportedContentType),

            ResourceAlreadyExists => nameof(ResourceAlreadyExists),
            OperationFailed => nameof(OperationFailed),
            DataProcessingFailed => nameof(DataProcessingFailed),
            InvalidStatusForOperation => nameof(InvalidStatusForOperation),
            OperationTooFrequent => nameof(OperationTooFrequent),
            ResourceExpired => nameof(ResourceExpired),

            Unauthorized => nameof(Unauthorized),
            InvalidToken => nameof(InvalidToken),
            TokenExpired => nameof(TokenExpired),
            LoginExpired => nameof(LoginExpired),
            RefreshTokenInvalid => nameof(RefreshTokenInvalid),
            InvalidCredentials => nameof(InvalidCredentials),
            OAuthCodeInvalid => nameof(OAuthCodeInvalid),
            OAuthProviderNotSupported => nameof(OAuthProviderNotSupported),
            VerificationCodeInvalid => nameof(VerificationCodeInvalid),
            RegistrationSessionExpired => nameof(RegistrationSessionExpired),
            OAuthAccountNotLinked => nameof(OAuthAccountNotLinked),
            OAuthAccountAlreadyLinked => nameof(OAuthAccountAlreadyLinked),
            LinkedAccountNotFound => nameof(LinkedAccountNotFound),

            InsufficientPermission => nameof(InsufficientPermission),
            PostEditDenied => nameof(PostEditDenied),
            PostDeleteDenied => nameof(PostDeleteDenied),
            CommentEditDenied => nameof(CommentEditDenied),
            CommentDeleteDenied => nameof(CommentDeleteDenied),
            AccountDisabled => nameof(AccountDisabled),
            AccountNotActive => nameof(AccountNotActive),

            UserNotFound => nameof(UserNotFound),
            AccountAlreadyExists => nameof(AccountAlreadyExists),
            UserEmailNotBound => nameof(UserEmailNotBound),
            UserPhoneNotBound => nameof(UserPhoneNotBound),
            EmailAlreadyRegistered => nameof(EmailAlreadyRegistered),
            OldPasswordMismatch => nameof(OldPasswordMismatch),
            PasswordMismatch => nameof(PasswordMismatch),
            CannotOperateSelf => nameof(CannotOperateSelf),
            NoRecoveryChannelAvailable => nameof(NoRecoveryChannelAvailable),
            UnsupportedVerificationMethod => nameof(UnsupportedVerificationMethod),
            UnsupportedOAuthPlatform => nameof(UnsupportedOAuthPlatform),
            OAuthNotBound => nameof(OAuthNotBound),

            PostNotFound => nameof(PostNotFound),
            PostCreateFailed => nameof(PostCreateFailed),
            PostUpdateFailed => nameof(PostUpdateFailed),

            CommentNotFound => nameof(CommentNotFound),
            ParentCommentNotFound => nameof(ParentCommentNotFound),
            CommentCreateFailed => nameof(CommentCreateFailed),
            CommentUpdateFailed => nameof(CommentUpdateFailed),

            CannotFollowSelf => nameof(CannotFollowSelf),
            CannotBlockSelf => nameof(CannotBlockSelf),
            CannotFollowBlockedUser => nameof(CannotFollowBlockedUser),
            FollowTooFrequent => nameof(FollowTooFrequent),
            BlockTooFrequent => nameof(BlockTooFrequent),

            FileNotFound => nameof(FileNotFound),
            UnsupportedFileType => nameof(UnsupportedFileType),
            FileUploadFailed => nameof(FileUploadFailed),
            FileSizeExceeded => nameof(FileSizeExceeded),

            LikeTooFrequent => nameof(LikeTooFrequent),
            AlreadyLiked => nameof(AlreadyLiked),
            NotLikedYet => nameof(NotLikedYet),

            InternalServerError => nameof(InternalServerError),
            DatabaseOperationFailed => nameof(DatabaseOperationFailed),
            CacheUnavailable => nameof(CacheUnavailable),
            CacheOperationFailed => nameof(CacheOperationFailed),
            NetworkError => nameof(NetworkError),

            ExternalServiceFailed => nameof(ExternalServiceFailed),
            ExternalServiceTimeout => nameof(ExternalServiceTimeout),
            ExternalServiceReturnError => nameof(ExternalServiceReturnError),
            ExternalServiceNotConfigured => nameof(ExternalServiceNotConfigured),
            EmailSendFailed => nameof(EmailSendFailed),
            SmsSendFailed => nameof(SmsSendFailed),

            TooManyRequests => nameof(TooManyRequests),
            IpBlacklisted => nameof(IpBlacklisted),

            _ => errorCode.ToString()
        };
    }
}
