using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketOurs.Data.DataModels;

public enum NotificationType
{
    CommentReply,
    PostReply,
    HotList,
    System
}

[Table("notifications")]
public class NotificationModel : DataModel
{
    [Key]
    [Required]
    [MaxLength(64)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(64)]
    public string UserId { get; set; } = "";
    
    [ForeignKey("UserId")]
    public UserModel User { get; set; } = null!;

    [Required]
    [MaxLength(128)]
    public string Title { get; set; } = "";

    [Required]
    [MaxLength(1024)]
    public string Content { get; set; } = "";

    public NotificationType Type { get; set; }

    [MaxLength(64)]
    public string? TargetId { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public override void Update(DataModel model)
    {
        if (model is not NotificationModel notification) return;
        
        IsRead = notification.IsRead;
    }
}
