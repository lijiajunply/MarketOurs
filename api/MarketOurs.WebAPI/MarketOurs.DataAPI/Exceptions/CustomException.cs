namespace MarketOurs.DataAPI.Exceptions;

/// <summary>
/// 自定义异常基类
/// </summary>
public class CustomException : Exception
{
    /// <summary>
    /// 错误代码
    /// </summary>
    public int ErrorCode { get; }
    
    /// <summary>
    /// HTTP状态码
    /// </summary>
    public int HttpStatusCode { get; }
    
    /// <summary>
    /// 详细描述
    /// </summary>
    public string? Detail { get; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误消息</param>
    /// <param name="httpStatusCode">HTTP状态码</param>
    /// <param name="detail">详细描述</param>
    /// <param name="innerException">内部异常</param>
    public CustomException(int errorCode, string message, int httpStatusCode = 500, string? detail = null, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        HttpStatusCode = httpStatusCode;
        Detail = detail;
    }
}
