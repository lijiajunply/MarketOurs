namespace MarketOurs.DataAPI.Exceptions;

/// <summary>
/// 资源访问异常
/// </summary>
public class ResourceAccessException : CustomException
{
    /// <summary>
    /// 资源名称
    /// </summary>
    public string? ResourceName { get; }
    
    /// <summary>
    /// 资源ID
    /// </summary>
    public string? ResourceId { get; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误消息</param>
    /// <param name="resourceName">资源名称</param>
    /// <param name="resourceId">资源ID</param>
    /// <param name="detail">详细描述</param>
    /// <param name="innerException">内部异常</param>
    public ResourceAccessException(int errorCode, string message, string? resourceName = null, string? resourceId = null, string? detail = null, Exception? innerException = null)
        : base(errorCode, message, 404, detail, innerException)
    {
        ResourceName = resourceName;
        ResourceId = resourceId;
    }
}
