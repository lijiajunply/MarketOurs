using System.ComponentModel.DataAnnotations;

namespace MarketOurs.Data.DTOs;

public class CommentDto
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Images { get; set; } = [];
    public int Likes { get; set; }
    public int Dislikes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string PostId { get; set; } = string.Empty;
    public string? ParentCommentId { get; set; }
    public List<CommentDto> RepliedComments { get; set; } = [];
}

public class CommentCreateDto
{
    [Required(ErrorMessage = "评论内容不能为空")] 
    [MaxLength(512, ErrorMessage = "评论内容长度不能超过512位")] 
    public string Content { get; set; } = string.Empty;

    public List<string> Images { get; set; } = [];

    [Required(ErrorMessage = "用户ID不能为空")]
    [MaxLength(64, ErrorMessage = "用户ID长度不能超过64位")]
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "贴子ID不能为空")]
    [MaxLength(64, ErrorMessage = "贴子ID长度不能超过64位")]
    public string PostId { get; set; } = string.Empty;

    [MaxLength(64, ErrorMessage = "父评论ID长度不能超过64位")]
    public string? ParentCommentId { get; set; }
}

public class CommentUpdateDto
{
    [Required(ErrorMessage = "评论内容不能为空")] 
    [MaxLength(512, ErrorMessage = "评论内容长度不能超过512位")] 
    public string Content { get; set; } = string.Empty;

    public List<string> Images { get; set; } = [];
}
