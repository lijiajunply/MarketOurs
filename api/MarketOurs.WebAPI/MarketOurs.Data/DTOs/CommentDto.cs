using System.ComponentModel.DataAnnotations;

namespace MarketOurs.Data.DTOs;

/// <summary>
/// 评论数据传输对象
/// </summary>
public class CommentDto
{
    /// <summary>
    /// 评论唯一标识
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 评论内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 评论中的图片列表
    /// </summary>
    public List<string> Images { get; set; } = [];

    /// <summary>
    /// 点赞数
    /// </summary>
    public int Likes { get; set; }

    /// <summary>
    /// 点踩数
    /// </summary>
    public int Dislikes { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 评论者用户 ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 评论者信息
    /// </summary>
    public UserSimpleDto? Author { get; set; }

    /// <summary>
    /// 所属帖子 ID
    /// </summary>
    public string PostId { get; set; } = string.Empty;

    /// <summary>
    /// 父评论 ID (如果是回复)
    /// </summary>
    public string? ParentCommentId { get; set; }

    /// <summary>
    /// 该评论下的回复列表 (树形结构)
    /// </summary>
    public List<CommentDto> RepliedComments { get; set; } = [];
}

/// <summary>
/// 创建评论请求对象
/// </summary>
public class CommentCreateDto
{
    /// <summary>
    /// 评论内容
    /// </summary>
    [Required(ErrorMessage = "评论内容不能为空")] 
    [MaxLength(512, ErrorMessage = "评论内容长度不能超过512位")] 
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 图片列表
    /// </summary>
    public List<string> Images { get; set; } = [];

    /// <summary>
    /// 用户 ID
    /// </summary>
    [Required(ErrorMessage = "用户ID不能为空")]
    [MaxLength(64, ErrorMessage = "用户ID长度不能超过64位")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 帖子 ID
    /// </summary>
    [Required(ErrorMessage = "贴子ID不能为空")]
    [MaxLength(64, ErrorMessage = "贴子ID长度不能超过64位")]
    public string PostId { get; set; } = string.Empty;

    /// <summary>
    /// 父评论 ID
    /// </summary>
    [MaxLength(64, ErrorMessage = "父评论ID长度不能超过64位")]
    public string? ParentCommentId { get; set; }
}

/// <summary>
/// 更新评论请求对象
/// </summary>
public class CommentUpdateDto
{
    /// <summary>
    /// 评论内容
    /// </summary>
    [Required(ErrorMessage = "评论内容不能为空")] 
    [MaxLength(512, ErrorMessage = "评论内容长度不能超过512位")] 
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 图片列表
    /// </summary>
    public List<string> Images { get; set; } = [];
}
