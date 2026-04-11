namespace MarketOurs.DataAPI.Exceptions;

/// <summary>
/// 业务逻辑异常
/// </summary>
public class BusinessException : CustomException
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误消息</param>
    /// <param name="detail">详细描述</param>
    /// <param name="innerException">内部异常</param>
    public BusinessException(int errorCode, string message, string? detail = null, Exception? innerException = null)
        : base(errorCode, message, 400, detail, innerException)
    {}

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误消息</param>
    /// <param name="httpStatusCode">HTTP状态码</param>
    /// <param name="detail">详细描述</param>
    /// <param name="innerException">内部异常</param>
    public BusinessException(int errorCode, string message, int httpStatusCode, string? detail, Exception? innerException = null)
        : base(errorCode, message, httpStatusCode, detail, innerException)
        {}
}
