using System.Collections.Concurrent;
using MarketOurs.Data.DataModels;

namespace MarketOurs.DataAPI.Services.Background;

public class NotificationMessage
{
    public string UserId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public NotificationType Type { get; set; }
    public string? TargetId { get; set; }
}

public class NotificationMessageQueue
{
    private readonly ConcurrentQueue<NotificationMessage> _queue = new();

    public void Enqueue(NotificationMessage message)
    {
        _queue.Enqueue(message);
    }

    public bool TryDequeue(out NotificationMessage? message)
    {
        return _queue.TryDequeue(out message);
    }

    public int Count => _queue.Count;
}
