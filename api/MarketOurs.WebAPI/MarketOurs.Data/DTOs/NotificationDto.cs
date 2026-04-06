using MarketOurs.Data.DataModels;

namespace MarketOurs.Data.DTOs;

/// <summary>
/// 通知数据传输对象
/// </summary>
public class NotificationDto
{
    /// <summary>
    /// 通知唯一标识
    /// </summary>
    public string Id { get; set; } = "";

    /// <summary>
    /// 接收通知的用户 ID
    /// </summary>
    public string UserId { get; set; } = "";

    /// <summary>
    /// 通知标题
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// 通知正文内容
    /// </summary>
    public string Content { get; set; } = "";

    /// <summary>
    /// 通知类型 (如评论回复、系统消息等)
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// 关联的对象 ID (如帖子 ID)
    /// </summary>
    public string? TargetId { get; set; }

    /// <summary>
    /// 是否已读
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 推送设置数据传输对象
/// </summary>
public class PushSettingsDto
{
    /// <summary>
    /// 是否启用邮件通知
    /// </summary>
    public bool EnableEmailNotifications { get; set; } = true;

    /// <summary>
    /// 是否推送热门榜单更新
    /// </summary>
    public bool EnableHotListPush { get; set; } = true;

    /// <summary>
    /// 是否推送评论回复提醒
    /// </summary>
    public bool EnableCommentReplyPush { get; set; } = true;
}
