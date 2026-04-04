using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketOurs.Data.DataModels;

public class UserModel : DataModel
{
    [Key]
    [Column(name: "id")]
    [MaxLength(64)]
    public string Id { get; set; } = "";

    [Required]
    [EmailAddress]
    [MaxLength(128)]
    public string Email { get; set; } = "";

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
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime LastLoginAt { get; set; } = DateTime.Now;

    public bool IsActive { get; set; } = true;

    public bool IsEmailVerified { get; set; } = false;

    public override void Update(DataModel model)
    {
        if (model is not UserModel userModel)
            return;
        Email = userModel.Email;
        Password = userModel.Password;
        Name = userModel.Name;
        Role = userModel.Role;
        Avatar = userModel.Avatar;
        Info = userModel.Info;
        IsActive = userModel.IsActive;
        IsEmailVerified = userModel.IsEmailVerified;
    }
}