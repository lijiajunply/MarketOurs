namespace MarketOurs.DataAPI.Exceptions;

/// <summary>
/// 数据访问异常
/// </summary>
public class DataAccessException : CustomException
{
    /// <summary>
    /// 数据库操作类型
    /// </summary>
    public string? OperationType { get; }
    
    /// <summary>
    /// 相关表名
    /// </summary>
    public string? TableName { get; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误消息</param>
    /// <param name="operationType">数据库操作类型</param>
    /// <param name="tableName">相关表名</param>
    /// <param name="detail">详细描述</param>
    /// <param name="innerException">内部异常</param>
    public DataAccessException(int errorCode, string message, string? operationType = null, string? tableName = null, string? detail = null, Exception? innerException = null)
        : base(errorCode, message, 500, detail, innerException)
    {
        OperationType = operationType;
        TableName = tableName;
    }
}