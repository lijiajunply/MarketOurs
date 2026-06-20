using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketOurs.Data.DataModels;

/// <summary>
/// 通知类型枚举
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// 评论回复
    /// </summary>
    CommentReply,

    /// <summary>
    /// 帖子收到了新评论
    /// </summary>
    PostReply,

    /// <summary>
    /// 热门榜单推送
    /// </summary>
    HotList,

    /// <summary>
    /// 系统全局消息
    /// </summary>
    System,
    
    /// <summary>
    /// 审核
    /// </summary>
    Review,
}

/// <summary>
/// 通知实体模型，对应数据库中的 notifications 表
/// </summary>
[Table("notifications")]
public class NotificationModel : DataModel
{
    /// <summary>
    /// 主键 ID
    /// </summary>
    [Key]
    [Required]
    [MaxLength(64)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 接收通知的用户 ID
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string UserId { get; set; } = "";
    
    /// <summary>
    /// 接收通知的用户实体
    /// </summary>
    [ForeignKey("UserId")]
    public UserModel User { get; set; } = null!;

    /// <summary>
    /// 通知标题
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string Title { get; set; } = "";

    /// <summary>
    /// 通知正文内容
    /// </summary>
    [Required]
    [MaxLength(1024)]
    public string Content { get; set; } = "";

    /// <summary>
    /// 通知类型
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// 关联的对象 ID (如帖子 ID)
    /// </summary>
    [MaxLength(64)]
    public string? TargetId { get; set; }

    /// <summary>
    /// 结构化参数 (JSON)，用于前端根据 NotificationType 进行多语言渲染
    /// </summary>
    [MaxLength(2048)]
    public string? Params { get; set; }

    /// <summary>
    /// 将数据库中的 JSON 字符串反序列化为强类型参数对象
    /// </summary>
    public NotificationParams? GetParams() =>
        !string.IsNullOrEmpty(Params)
            ? System.Text.Json.JsonSerializer.Deserialize<NotificationParams>(Params)
            : null;

    /// <summary>
    /// 将强类型参数对象序列化为 JSON 字符串写入数据库字段
    /// </summary>
    public void SetParams(NotificationParams? value) =>
        Params = value != null ? System.Text.Json.JsonSerializer.Serialize(value) : null;

    /// <summary>
    /// 是否已读
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新实体属性
    /// </summary>
    public override void Update(DataModel model)
    {
        if (model is not NotificationModel notification) return;
        
        IsRead = notification.IsRead;
    }
}
