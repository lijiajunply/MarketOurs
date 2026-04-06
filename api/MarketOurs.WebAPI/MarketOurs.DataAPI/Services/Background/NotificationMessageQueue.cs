using System.Collections.Concurrent;
using MarketOurs.Data.DataModels;

namespace MarketOurs.DataAPI.Services.Background;

/// <summary>
/// 异步通知消息载荷
/// </summary>
public class NotificationMessage
{
    /// <summary>
    /// 接收通知的用户 ID
    /// </summary>
    public string UserId { get; set; } = "";

    /// <summary>
    /// 通知标题
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// 通知详情内容
    /// </summary>
    public string Content { get; set; } = "";

    /// <summary>
    /// 通知业务类型 (如评论回复等)
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// 关联的业务对象 ID (可选)
    /// </summary>
    public string? TargetId { get; set; }
}

/// <summary>
/// 基于 ConcurrentQueue 实现的进程内通知异步消息队列
/// </summary>
public class NotificationMessageQueue
{
    private readonly ConcurrentQueue<NotificationMessage> _queue = new();

    /// <summary>
    /// 将消息压入队列
    /// </summary>
    /// <param name="message">通知消息对象</param>
    public void Enqueue(NotificationMessage message)
    {
        _queue.Enqueue(message);
    }

    /// <summary>
    /// 尝试从队列中弹出一个消息
    /// </summary>
    /// <param name="message">输出的消息对象</param>
    /// <returns>是否成功获取消息</returns>
    public bool TryDequeue(out NotificationMessage? message)
    {
        return _queue.TryDequeue(out message);
    }

    /// <summary>
    /// 获取当前队列中积压的消息总数
    /// </summary>
    public int Count => _queue.Count;
}
