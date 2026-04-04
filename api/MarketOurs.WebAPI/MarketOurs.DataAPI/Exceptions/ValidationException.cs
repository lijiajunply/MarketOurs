namespace MarketOurs.DataAPI.Exceptions;

/// <summary>
/// 数据验证异常
/// </summary>
public class ValidationException : CustomException
{
    /// <summary>
    /// 验证错误详情
    /// </summary>
    public IDictionary<string, string[]>? ValidationErrors { get; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误消息</param>
    /// <param name="validationErrors">验证错误详情</param>
    /// <param name="detail">详细描述</param>
    /// <param name="innerException">内部异常</param>
    public ValidationException(int errorCode, string message, IDictionary<string, string[]>? validationErrors = null, string? detail = null, Exception? innerException = null)
        : base(errorCode, message, 400, detail, innerException)
    {
        ValidationErrors = validationErrors;
    }
}
