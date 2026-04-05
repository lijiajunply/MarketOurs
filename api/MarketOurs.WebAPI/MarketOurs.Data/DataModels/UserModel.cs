using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketOurs.Data.DataModels;

[Table("users")]
public class UserModel : DataModel
{
    [Key]
    [Column(name: "id")]
    [MaxLength(64)]
    public string Id { get; set; } = "";

    [MaxLength(128)]
    public string Email { get; set; } = "";

    [MaxLength(32)]
    public string Phone { get; set; } = "";

    [Required] [MaxLength(128)] public string Password { get; set; } = "";

    [Required] [MaxLength(128)] public string Name { get; set; } = "";

    /// <summary>
    /// 分为 User、Admin、School 三种
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string Role { get; set; } = "User";

    [MaxLength(128)] public string Avatar { get; set; } = "";

    [MaxLength(1024)] public string Info { get; set; } = "";

    public List<CommentModel> Comments { get; set; } = [];
    
    public List<PostModel> Posts { get; set; } = [];
    
    public List<PostModel> LikePosts { get; set; } = [];
    public List<CommentModel> LikeComments { get; set; } = [];
    
    public List<PostModel> DislikesPosts { get; set; } = [];
    public List<CommentModel> DislikesComments { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public bool IsEmailVerified { get; set; }
    
    public bool IsPhoneVerified { get; set; }

    /// <summary>
    /// 推送设置 (JSON 存储)
    /// </summary>
    [MaxLength(2048)]
    public string? PushSettings { get; set; }

    /// <summary>
    /// 移动端推送 Token (如 FCM Token)
    /// </summary>
    [MaxLength(2048)]
    public string? PushToken { get; set; }

    public override void Update(DataModel model)
    {
        if (model is not UserModel userModel)
            return;
        Email = userModel.Email;
        Phone = userModel.Phone;
        Password = userModel.Password;
        Name = userModel.Name;
        Role = userModel.Role;
        Avatar = userModel.Avatar;
        Info = userModel.Info;
        IsActive = userModel.IsActive;
        IsEmailVerified = userModel.IsEmailVerified;
        IsPhoneVerified = userModel.IsPhoneVerified;
        PushSettings = userModel.PushSettings;
        PushToken = userModel.PushToken;
    }
}