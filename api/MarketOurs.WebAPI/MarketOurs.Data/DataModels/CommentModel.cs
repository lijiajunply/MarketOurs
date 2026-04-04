using System.ComponentModel.DataAnnotations;

namespace MarketOurs.Data.DataModels;

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
    public List<CommentModel> Comments = [];
    public UserModel User = new();
    
    public List<UserModel> LikeUsers = [];
    public List<UserModel> DislikeUsers = [];
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    [MaxLength(64)]
    public string UserId { get; set; } = "";
    
    public PostModel Post = new();
    
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