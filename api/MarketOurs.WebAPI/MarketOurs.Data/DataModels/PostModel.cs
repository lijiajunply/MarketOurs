using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketOurs.Data.DataModels;

[Table("posts")]
public class PostModel : DataModel
{
    [Key] [Required] [MaxLength(64)] public string Id { get; set; } = "";

    [Required] [MaxLength(128)] public string Title { get; set; } = "";

    [Required] [MaxLength(1024)] public string Content { get; set; } = "";
    public List<string> Images { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    [MaxLength(64)] public string UserId { get; set; } = "";
    public UserModel User { get; set; } = new();

    public List<CommentModel> Comments = [];

    public List<UserModel> LikeUsers = [];
    public List<UserModel> DislikeUsers = [];
    public int Likes { get; set; }
    public int Dislikes { get; set; }

    public int Watch { get; set; }

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