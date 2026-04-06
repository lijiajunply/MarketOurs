namespace MarketOurs.DataAPI.Exceptions;

/// <summary>
/// 业务错误码定义
/// </summary>
public static class ErrorCode
{
    #region 成功 (0)

    /// <summary>
    /// 成功
    /// </summary>
    public const int Success = 0;

    #endregion

    #region 参数错误 (1000-1999)

    /// <summary>
    /// 参数不能为空
    /// </summary>
    public const int ParameterEmpty = 1000;

    /// <summary>
    /// 参数格式错误
    /// </summary>
    public const int ParameterFormatError = 1001;

    /// <summary>
    /// 参数超出范围
    /// </summary>
    public const int ParameterOutOfRange = 1002;

    /// <summary>
    /// 参数验证失败
    /// </summary>
    public const int ParameterValidationFailed = 1003;

    #endregion

    #region 业务逻辑错误 (2000-2999)

    /// <summary>
    /// 资源已存在
    /// </summary>
    public const int ResourceAlreadyExists = 2000;

    /// <summary>
    /// 操作失败
    /// </summary>
    public const int OperationFailed = 2001;

    /// <summary>
    /// 数据处理失败
    /// </summary>
    public const int DataProcessingFailed = 2002;

    /// <summary>
    /// 状态不允许该操作
    /// </summary>
    public const int InvalidStatusForOperation = 2003;

    #endregion

    #region 权限错误 (3000-3999)

    /// <summary>
    /// 未授权访问
    /// </summary>
    public const int Unauthorized = 3000;

    /// <summary>
    /// 权限不足
    /// </summary>
    public const int InsufficientPermission = 3001;

    /// <summary>
    /// 登录已过期
    /// </summary>
    public const int LoginExpired = 3002;

    /// <summary>
    /// 无效的令牌
    /// </summary>
    public const int InvalidToken = 3003;

    #endregion

    #region 资源错误 (4000-4999)

    /// <summary>
    /// 资源不存在
    /// </summary>
    public const int ResourceNotFound = 4000;

    /// <summary>
    /// 文章不存在
    /// </summary>
    public const int ArticleNotFound = 4001;

    /// <summary>
    /// 分类不存在
    /// </summary>
    public const int CategoryNotFound = 4002;

    /// <summary>
    /// 用户不存在
    /// </summary>
    public const int UserNotFound = 4003;

    /// <summary>
    /// 项目不存在
    /// </summary>
    public const int ProjectNotFound = 4004;

    /// <summary>
    /// 文件不存在
    /// </summary>
    public const int FileNotFound = 4005;

    /// <summary>
    /// 用户被封
    /// </summary>
    public const int UserLocked = 4006;

    /// <summary>
    /// 用户未激活
    /// </summary>
    public const int UserNotActive = 4007;

    #endregion

    #region 系统错误 (5000-5999)

    /// <summary>
    /// 服务器内部错误
    /// </summary>
    public const int InternalServerError = 5000;

    /// <summary>
    /// 数据库操作失败
    /// </summary>
    public const int DatabaseOperationFailed = 5001;

    /// <summary>
    /// 缓存操作失败
    /// </summary>
    public const int CacheOperationFailed = 5002;

    /// <summary>
    /// 网络错误
    /// </summary>
    public const int NetworkError = 5003;

    #endregion

    #region 外部服务错误 (6000-6999)

    /// <summary>
    /// 外部服务调用失败
    /// </summary>
    public const int ExternalServiceFailed = 6000;

    /// <summary>
    /// 外部服务超时
    /// </summary>
    public const int ExternalServiceTimeout = 6001;

    /// <summary>
    /// 外部服务返回错误
    /// </summary>
    public const int ExternalServiceReturnError = 6002;

    /// <summary>
    /// 外部服务未配置
    /// </summary>
    public const int ExternalServiceNotConfigured = 6003;

    #endregion

    #region HTTP状态码相关错误 (7000-7999)

    /// <summary>
    /// 请求频率过高
    /// </summary>
    public const int TooManyRequests = 7000;

    /// <summary>
    /// 无效请求
    /// </summary>
    public const int InvalidRequest = 7001;

    #endregion
}