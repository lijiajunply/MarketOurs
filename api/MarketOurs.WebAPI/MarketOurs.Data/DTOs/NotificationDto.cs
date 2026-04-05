using MarketOurs.Data.DataModels;

namespace MarketOurs.Data.DTOs;

public class NotificationDto
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public NotificationType Type { get; set; }
    public string? TargetId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PushSettingsDto
{
    public bool EnableEmailNotifications { get; set; } = true;
    public bool EnableHotListPush { get; set; } = true;
    public bool EnableCommentReplyPush { get; set; } = true;
}
