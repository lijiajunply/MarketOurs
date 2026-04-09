using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketOurs.Data.DataModels;

/// <summary>
/// 帖子实体模型，对应数据库中的 posts 表
/// </summary>
[Table("posts")]
public class PostModel : DataModel
{
    /// <summary>
    /// 主键 ID
    /// </summary>
    [Key] [Required] [MaxLength(64)] public string Id { get; set; } = "";

    /// <summary>
    /// 标题
    /// </summary>
    [Required] [MaxLength(128)] public string Title { get; set; } = "";

    /// <summary>
    /// 内容
    /// </summary>
    [Required] [MaxLength(1024)] public string Content { get; set; } = "";

    /// <summary>
    /// 图片列表 (JSON 序列化存储)
    /// </summary>
    public List<string> Images { get; set; } = [];

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 发布者用户 ID
    /// </summary>
    [MaxLength(64)] public string UserId { get; set; } = "";

    /// <summary>
    /// 发布者用户实体
    /// </summary>
    public UserModel User { get; set; } = null!;

    /// <summary>
    /// 帖子的评论列表
    /// </summary>
    public List<CommentModel> Comments { get; set; } = [];

    /// <summary>
    /// 点赞的用户列表
    /// </summary>
    public List<UserModel> LikeUsers { get; set; } = [];

    /// <summary>
    /// 点踩的用户列表
    /// </summary>
    public List<UserModel> DislikeUsers { get; set; } = [];

    /// <summary>
    /// 点赞总数 (持久化缓存)
    /// </summary>
    public int Likes { get; set; }

    /// <summary>
    /// 点踩总数 (持久化缓存)
    /// </summary>
    public int Dislikes { get; set; }

    /// <summary>
    /// 浏览量
    /// </summary>
    public int Watch { get; set; }

    /// <summary>
    /// 是否通过审核
    /// </summary>
    public bool IsReview { get; set; }

    /// <summary>
    /// 更新实体属性
    /// </summary>
    /// <param name="model">源模型</param>
    public override void Update(DataModel model)
    {
        if (model is not PostModel postModel) return;

        Id = postModel.Id;
        Title = postModel.Title;
        Content = postModel.Content;
        Images = postModel.Images;
        CreatedAt = postModel.CreatedAt;
        UpdatedAt = postModel.UpdatedAt;
        UserId = postModel.UserId;

        LikeUsers = postModel.LikeUsers;
        DislikeUsers = postModel.DislikeUsers;
        Likes = postModel.Likes == 0 ? postModel.LikeUsers.Count : postModel.Likes;
        Dislikes = postModel.Dislikes == 0 ? postModel.DislikeUsers.Count : postModel.Dislikes;
        Watch = postModel.Watch;
    }
}