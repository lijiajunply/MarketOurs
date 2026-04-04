namespace MarketOurs.DataAPI.Exceptions;

/// <summary>
/// 认证授权异常
/// </summary>
public class AuthException : CustomException
{
    /// <summary>
    /// 认证类型
    /// </summary>
    public string? AuthType { get; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误消息</param>
    /// <param name="httpStatusCode">HTTP状态码</param>
    /// <param name="authType">认证类型</param>
    /// <param name="detail">详细描述</param>
    /// <param name="innerException">内部异常</param>
    public AuthException(int errorCode, string message, int httpStatusCode = 401, string? authType = null, string? detail = null, Exception? innerException = null)
        : base(errorCode, message, httpStatusCode, detail, innerException)
    {
        AuthType = authType;
    }
}
