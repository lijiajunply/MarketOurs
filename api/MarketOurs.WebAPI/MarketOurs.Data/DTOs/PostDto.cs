using System.ComponentModel.DataAnnotations;

namespace MarketOurs.Data.DTOs;

public class PostDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Images { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int Likes { get; set; }
    public int Dislikes { get; set; }
    public int Watch { get; set; }
}

public class PostCreateDto
{
    [Required(ErrorMessage = "标题不能为空")] 
    [MaxLength(128, ErrorMessage = "标题长度不能超过128位")] 
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "内容不能为空")] 
    [MaxLength(1024, ErrorMessage = "内容长度不能超过1024位")] 
    public string Content { get; set; } = string.Empty;

    public List<string> Images { get; set; } = [];

    [Required(ErrorMessage = "用户ID不能为空")] 
    [MaxLength(64, ErrorMessage = "用户ID长度不能超过64位")]
    public string UserId { get; set; } = string.Empty;
}

public class PostUpdateDto
{
    [Required(ErrorMessage = "标题不能为空")] 
    [MaxLength(128, ErrorMessage = "标题长度不能超过128位")] 
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "内容不能为空")] 
    [MaxLength(1024, ErrorMessage = "内容长度不能超过1024位")] 
    public string Content { get; set; } = string.Empty;

    public List<string> Images { get; set; } = [];
}
