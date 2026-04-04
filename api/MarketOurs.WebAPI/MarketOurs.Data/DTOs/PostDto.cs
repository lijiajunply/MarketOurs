using System;
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
    [Required] public string Title { get; set; } = string.Empty;
    [Required] public string Content { get; set; } = string.Empty;
    public List<string> Images { get; set; } = [];
    [Required] public string UserId { get; set; } = string.Empty;
}

public class PostUpdateDto
{
    [Required] public string Title { get; set; } = string.Empty;
    [Required] public string Content { get; set; } = string.Empty;
    public List<string> Images { get; set; } = [];
}
