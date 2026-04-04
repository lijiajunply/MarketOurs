using System;
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
}

public class CommentCreateDto
{
    [Required] public string Content { get; set; } = string.Empty;
    public List<string> Images { get; set; } = [];
    public string UserId { get; set; } = string.Empty;
    public string PostId { get; set; } = string.Empty;
    public string? ParentCommentId { get; set; }
}

public class CommentUpdateDto
{
    [Required] public string Content { get; set; } = string.Empty;
    public List<string> Images { get; set; } = [];
}
