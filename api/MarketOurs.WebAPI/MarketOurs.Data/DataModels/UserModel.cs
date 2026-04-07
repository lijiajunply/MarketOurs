using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketOurs.Data.DataModels;

/// <summary>
/// 用户实体模型，对应数据库中的 users 表
/// </summary>
[Table("users")]
public class UserModel : DataModel
{
    /// <summary>
    /// 主键 ID
    /// </summary>
    [Key]
    [Column(name: "id")]
    [MaxLength(64)]
    public string Id { get; set; } = "";

    /// <summary>
    /// 电子邮箱
    /// </summary>
    [MaxLength(128)]
    public string Email { get; set; } = "";

    /// <summary>
    /// 手机号码
    /// </summary>
    [MaxLength(32)]
    public string Phone { get; set; } = "";

    /// <summary>
    /// 哈希后的密码
    /// </summary>
    [Required] [MaxLength(128)] public string Password { get; set; } = "";

    /// <summary>
    /// 用户名
    /// </summary>
    [Required] [MaxLength(128)] public string Name { get; set; } = "";

    /// <summary>
    /// 用户角色 (User, Admin, School)
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string Role { get; set; } = "User";

    /// <summary>
    /// 头像图片地址
    /// </summary>
    [MaxLength(128)] public string Avatar { get; set; } = "";

    /// <summary>
    /// 个人简介
    /// </summary>
    [MaxLength(1024)] public string Info { get; set; } = "";

    /// <summary>
    /// 用户发表过的评论列表
    /// </summary>
    public List<CommentModel> Comments { get; set; } = [];
    
    /// <summary>
    /// 用户发表过的帖子列表
    /// </summary>
    public List<PostModel> Posts { get; set; } = [];
    
    /// <summary>
    /// 用户点赞过的帖子列表
    /// </summary>
    public List<PostModel> LikePosts { get; set; } = [];

    /// <summary>
    /// 用户点赞过的评论列表
    /// </summary>
    public List<CommentModel> LikeComments { get; set; } = [];
    
    /// <summary>
    /// 用户点踩过的帖子列表
    /// </summary>
    public List<PostModel> DislikesPosts { get; set; } = [];

    /// <summary>
    /// 用户点踩过的评论列表
    /// </summary>
    public List<CommentModel> DislikesComments { get; set; } = [];

    /// <summary>
    /// 账户创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最后一次登录时间
    /// </summary>
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 账号是否激活/可用
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 邮箱是否已通过验证
    /// </summary>
    public bool IsEmailVerified { get; set; }
    
    /// <summary>
    /// 手机号是否已通过验证
    /// </summary>
    public bool IsPhoneVerified { get; set; }

    /// <summary>
    /// 推送设置 (JSON 序列化存储)
    /// </summary>
    [MaxLength(2048)]
    public string? PushSettings { get; set; }

    /// <summary>
    /// 移动端推送 Token (如 FCM Token)
    /// </summary>
    [MaxLength(2048)]
    public string? PushToken { get; set; }

    /// <summary>
    /// 第三方平台绑定信息
    /// </summary>
    [MaxLength(64)] public string? GithubId { get; set; }
    [MaxLength(64)] public string? GoogleId { get; set; }
    [MaxLength(64)] public string? WeixinId { get; set; }
    [MaxLength(64)] public string? OursId { get; set; }

    /// <summary>
    /// 更新实体属性
    /// </summary>
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
        GithubId = userModel.GithubId;
        GoogleId = userModel.GoogleId;
        WeixinId = userModel.WeixinId;
        OursId = userModel.OursId;
    }
}