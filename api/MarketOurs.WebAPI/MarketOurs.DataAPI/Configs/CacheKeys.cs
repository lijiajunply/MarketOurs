namespace MarketOurs.DataAPI.Configs;

/// <summary>
/// 统一管理全站缓存 Key
/// </summary>
[Serializable]
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

    #region Upload (上传相关)

    // 上传密钥 tracking (Redis) - Key 为 GUID, Value 为 JSON { urls: [...], createdAt: "..." }
    public static string UploadKey(string key) => $"upload_key:{key}";
    public static string UploadKeyPattern() => "upload_key:*";

    #endregion

    #region Auth (身份验证相关)

    // 用户访问令牌 (Redis)
    public static string UserAccessToken(string userId, string deviceType) => $"access_token:{userId}_{deviceType}";
    public static string UserRefreshToken(string refreshToken) => $"refresh_token:{refreshToken}";

    // 邮箱验证码 (Redis) - Key 为验证码，Value 为 UserId
    public static string VerificationToken(string token) => $"verify_token:{token}";

    // 重置密码验证码 (Redis) - Key 为验证码，Value 为 UserId
    public static string ResetToken(string token) => $"reset_token:{token}";

    // 预注册信息 (Redis) - Key 为 RegistrationToken, Value 为 UserCreateDto JSON
    public static string PreRegisterData(string token) => $"pre_reg_data:{token}";

    // 注册验证码 (Redis) - Key 为 RegistrationToken, Value 为 Code
    public static string RegistrationCode(string token) => $"reg_code:{token}";

    // 登录验证码 (Redis) - Key 为 Account, Value 为 Code
    public static string LoginCode(string account) => $"login_code:{account}";

    // 滑块验证码挑战 (Redis) - Key 为 ChallengeToken, Value 为 "puzzleX:puzzleY"
    public static string CaptchaChallenge(string token) => $"captcha_challenge:{token}";

    // 滑块验证码通过令牌 (Redis) - Key 为 CaptchaToken, Value 为 "1"
    public static string CaptchaToken(string token) => $"captcha_token:{token}";

    #endregion

    #region Follow & Block (关注/屏蔽相关 - Redis Set)

    // 关注关系集合
    public static string UserFollowing(string userId) => $"user:{userId}:following";
    public static string UserFollowers(string userId) => $"user:{userId}:followers";

    // 屏蔽关系集合
    public static string UserBlocked(string userId) => $"user:{userId}:blocked";

    // 关注统计 (本地缓存)
    public static string FollowStats(string userId) => $"user:{userId}:follow_stats";

    #endregion
}
