using System.ComponentModel.DataAnnotations;

namespace MarketOurs.Data.DTOs;

/// <summary>
/// 帖子数据传输对象
/// </summary>
public class PostDto
{
    /// <summary>
    /// 帖子唯一标识
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 图片列表
    /// </summary>
    public List<string> Images { get; set; } = [];

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 创建者ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 创建者信息
    /// </summary>
    public UserSimpleDto? Author { get; set; }

    /// <summary>
    /// 点赞数
    /// </summary>
    public int Likes { get; set; }

    /// <summary>
    /// 点踩数
    /// </summary>
    public int Dislikes { get; set; }

    /// <summary>
    /// 浏览量
    /// </summary>
    public int Watch { get; set; }
}

/// <summary>
/// 创建帖子请求对象
/// </summary>
public class PostCreateDto
{
    /// <summary>
    /// 标题
    /// </summary>
    [Required(ErrorMessage = "标题不能为空")] 
    [MaxLength(128, ErrorMessage = "标题长度不能超过128位")] 
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 内容
    /// </summary>
    [Required(ErrorMessage = "内容不能为空")] 
    [MaxLength(1024, ErrorMessage = "内容长度不能超过1024位")] 
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 图片列表
    /// </summary>
    public List<string> Images { get; set; } = [];

    /// <summary>
    /// 用户ID
    /// </summary>
    [Required(ErrorMessage = "用户ID不能为空")] 
    [MaxLength(64, ErrorMessage = "用户ID长度不能超过64位")]
    public string UserId { get; set; } = string.Empty;
}

/// <summary>
/// 更新帖子请求对象
/// </summary>
public class PostUpdateDto
{
    /// <summary>
    /// 标题
    /// </summary>
    [Required(ErrorMessage = "标题不能为空")] 
    [MaxLength(128, ErrorMessage = "标题长度不能超过128位")] 
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 内容
    /// </summary>
    [Required(ErrorMessage = "内容不能为空")] 
    [MaxLength(1024, ErrorMessage = "内容长度不能超过1024位")] 
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 图片列表
    /// </summary>
    public List<string> Images { get; set; } = [];
}
