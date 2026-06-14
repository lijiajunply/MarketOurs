using System.ComponentModel.DataAnnotations;

namespace MarketOurs.Data.DTOs;

public class PostTagDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#64748b";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PostTagCreateDto
{
    [Required(ErrorMessage = "标签名称不能为空")]
    [MaxLength(32, ErrorMessage = "标签名称长度不能超过32位")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(32, ErrorMessage = "标签颜色长度不能超过32位")]
    public string Color { get; set; } = "#64748b";
}

public class PostTagUpdateDto
{
    [Required(ErrorMessage = "标签名称不能为空")]
    [MaxLength(32, ErrorMessage = "标签名称长度不能超过32位")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(32, ErrorMessage = "标签颜色长度不能超过32位")]
    public string Color { get; set; } = "#64748b";

    public bool IsActive { get; set; } = true;
}

public class UpdatePostTagRequest
{
    public string? TagId { get; set; }
}
