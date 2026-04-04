using System;
using System.ComponentModel.DataAnnotations;

namespace MarketOurs.Data.DTOs;

public class CommentDto
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Likes { get; set; }
    public int Dislikes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string PostId { get; set; } = string.Empty;
}

public class CommentCreateDto
{
    [Required] public string Content { get; set; } = string.Empty;
    [Required] public string UserId { get; set; } = string.Empty;
    public string PostId { get; set; } = string.Empty;
}

public class CommentUpdateDto
{
    [Required] public string Content { get; set; } = string.Empty;
}
