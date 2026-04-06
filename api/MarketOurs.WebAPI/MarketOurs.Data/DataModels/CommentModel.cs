using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketOurs.Data.DataModels;

/// <summary>
/// 评论实体模型，对应数据库中的 comments 表
/// </summary>
[Table("comments")]
public class CommentModel : DataModel
{
    /// <summary>
    /// 主键 ID
    /// </summary>
    [Key]
    [Required]
    [MaxLength(64)]
    public string Id { get; set; } = "";
    
    /// <summary>
    /// 评论内容
    /// </summary>
    [Required]
    [MaxLength(512)]
    public string Content { get; set; } = "";

    /// <summary>
    /// 图片列表 (JSON 序列化存储)
    /// </summary>
    public List<string> Images { get; set; } = [];

    /// <summary>
    /// 点赞总数
    /// </summary>
    public int Likes { get; set; }

    /// <summary>
    /// 点踩总数
    /// </summary>
    public int Dislikes { get; set; }

    /// <summary>
    /// 子评论列表 (用于树形结构)
    /// </summary>
    public List<CommentModel> Comments { get; set; } = [];

    /// <summary>
    /// 评论者用户实体
    /// </summary>
    public UserModel User { get; set; } = null!;
    
    /// <summary>
    /// 点赞的用户列表
    /// </summary>
    public List<UserModel> LikeUsers { get; set; } = [];

    /// <summary>
    /// 点踩的用户列表
    /// </summary>
    public List<UserModel> DislikeUsers { get; set; } = [];

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 评论者用户 ID
    /// </summary>
    [MaxLength(64)]
    public string UserId { get; set; } = "";
    
    /// <summary>
    /// 所属帖子实体
    /// </summary>
    public PostModel Post { get; set; } = null!;
    
    /// <summary>
    /// 所属帖子 ID
    /// </summary>
    [MaxLength(64)]
    public string PostId { get; set; } = "";
    
    /// <summary>
    /// 父评论 ID (如果是回复)
    /// </summary>
    [MaxLength(64)]
    public string? ParentCommentId { get; set; }
    
    /// <summary>
    /// 父评论实体
    /// </summary>
    public CommentModel? ParentComment { get; set; }
    
    /// <summary>
    /// 更新实体属性
    /// </summary>
    public override void Update(DataModel model)
    {
        if (model is not CommentModel commitModel)
        {
            return;
        }
        
        Id = commitModel.Id;
        Content = commitModel.Content;
        Images = commitModel.Images;
        Likes = commitModel.Likes;
        Dislikes = commitModel.Dislikes;
    }
}