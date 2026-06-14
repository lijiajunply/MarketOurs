using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketOurs.Data.DataModels;

/// <summary>
/// 帖子标签实体模型，对应数据库中的 post_tags 表
/// </summary>
[Table("post_tags")]
public class PostTagModel : DataModel
{
    [Key] [Required] [MaxLength(64)] public string Id { get; set; } = "";

    [Required] [MaxLength(32)] public string Name { get; set; } = "";

    [Required] [MaxLength(32)] public string Color { get; set; } = "#64748b";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public override void Update(DataModel model)
    {
        if (model is not PostTagModel tagModel) return;

        Id = tagModel.Id;
        Name = tagModel.Name;
        Color = tagModel.Color;
        IsActive = tagModel.IsActive;
        CreatedAt = tagModel.CreatedAt;
        UpdatedAt = tagModel.UpdatedAt;
    }
}
