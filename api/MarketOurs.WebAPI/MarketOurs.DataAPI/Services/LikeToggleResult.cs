namespace MarketOurs.DataAPI.Services;

/// <summary>
/// 点赞/点踩切换操作的结果
/// </summary>
/// <param name="IsLiked">操作后用户是否点赞了该目标</param>
/// <param name="IsDisliked">操作后用户是否点踩了该目标</param>
/// <param name="LikeCount">操作后的点赞总数</param>
/// <param name="DislikeCount">操作后的点踩总数</param>
public record LikeToggleResult(bool IsLiked, bool IsDisliked, int LikeCount, int DislikeCount);
