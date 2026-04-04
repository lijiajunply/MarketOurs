namespace MarketOurs.DataAPI.Configs;

/// <summary>
/// 统一管理全站缓存 Key
/// </summary>
public static class CacheKeys
{
    #region Posts (帖子相关)

    // 帖子详情 (本地/分布式)
    public static string PostMem(string id) => $"post_mem_{id}";
    public static string PostDist(string id) => $"post_dist_{id}";

    // 热门列表 (本地/分布式)
    public static string HotPostsMem(int count) => $"hot_posts_mem_{count}";
    public static string HotPostsDist(int count) => $"hot_posts_dist_{count}";

    // 帖子下的评论列表 (本地)
    public static string PostComments(string postId) => $"post_comments_{postId}";

    // 帖子浏览量计数器 (Redis)
    public static string PostWatch(string id) => $"post:{id}:watch";

    #endregion

    #region Comments (评论相关)

    // 评论详情 (本地/分布式)
    public static string CommentMem(string id) => $"comment_mem_{id}";
    public static string CommentDist(string id) => $"comment_dist_{id}";

    #endregion

    #region Likes & Dislikes (点赞/踩相关 - Redis Set)

    public static string PostLikes(string id) => $"post:{id}:likes";
    public static string PostDislikes(string id) => $"post:{id}:dislikes";
    public static string CommentLikes(string id) => $"comment:{id}:likes";
    public static string CommentDislikes(string id) => $"comment:{id}:dislikes";

    #endregion

    #region Auth (身份验证相关)

    // 用户访问令牌 (Redis)
    public static string UserAccessToken(string userId, string deviceType) => $"access_token:{userId}_{deviceType}";

    // 邮箱验证码 (Redis) - Key 为验证码，Value 为 UserId
    public static string VerificationToken(string token) => $"verify_token:{token}";

    // 重置密码验证码 (Redis) - Key 为验证码，Value 为 UserId
    public static string ResetToken(string token) => $"reset_token:{token}";

    #endregion
}
