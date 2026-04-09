using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketOurs.Data.DataModels;

[Table("sensitive_words")]
public class SensitiveWordModel : DataModel
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(128)]
    public string Word { get; set; } = string.Empty;

    [MaxLength(64)]
    public string Category { get; set; } = "default";

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public override void Update(DataModel model)
    {
        if (model is not SensitiveWordModel sensitiveWordModel)
        {
            return;
        }

        Word = sensitiveWordModel.Word;
        Category = sensitiveWordModel.Category;
        IsEnabled = sensitiveWordModel.IsEnabled;
        UpdatedAt = sensitiveWordModel.UpdatedAt;
    }
}
