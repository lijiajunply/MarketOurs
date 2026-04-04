using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketOurs.Data.DataModels;

[Table("comments")]
public class CommentModel : DataModel
{
    [Key]
    [Required]
    [MaxLength(64)]
    public string Id { get; set; } = "";
    
    [Required]
    [MaxLength(512)]
    public string Content { get; set; } = "";
    public List<string> Images { get; set; } = [];
    public int Likes { get; set; }
    public int Dislikes { get; set; }
    public List<CommentModel> Comments { get; set; } = [];
    public UserModel User { get; set; } = null!;
    
    public List<UserModel> LikeUsers { get; set; } = [];
    public List<UserModel> DislikeUsers { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(64)]
    public string UserId { get; set; } = "";
    
    public PostModel Post { get; set; } = null!;
    
    [MaxLength(64)]
    public string PostId { get; set; } = "";
    
    [MaxLength(64)]
    public string? ParentCommentId { get; set; }
    
    public CommentModel? ParentComment { get; set; }
    
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