using System.Threading.Channels;

namespace MarketOurs.DataAPI.Services.Background;

/// <summary>
/// 互动操作目标类型
/// </summary>
public enum TargetType
{
    /// <summary>
    /// 帖子
    /// </summary>
    Post,

    /// <summary>
    /// 评论
    /// </summary>
    Comment
}

/// <summary>
/// 互动操作行为类型
/// </summary>
public enum ActionType
{
    /// <summary>
    /// 点赞
    /// </summary>
    Like,

    /// <summary>
    /// 点踩
    /// </summary>
    Dislike,

    /// <summary>
    /// 取消点赞
    /// </summary>
    Unlike,

    /// <summary>
    /// 取消点踩
    /// </summary>
    Undislike
}

/// <summary>
/// 点赞/点踩互动消息载荷
/// </summary>
/// <param name="Target">目标类型 (帖子/评论)</param>
/// <param name="Action">操作行为</param>
/// <param name="TargetId">目标 ID</param>
/// <param name="UserId">执行操作的用户 ID</param>
public record LikeMessage(TargetType Target, ActionType Action, string TargetId, string UserId);

/// <summary>
/// 基于 System.Threading.Channels 实现的高性能点赞异步处理队列
/// </summary>
public class LikeMessageQueue
{
    private readonly Channel<LikeMessage> _channel = Channel.CreateUnbounded<LikeMessage>();

    /// <summary>
    /// 将互动消息写入队列
    /// </summary>
    /// <param name="message">互动消息对象</param>
    public virtual async ValueTask EnqueueAsync(LikeMessage message)
    {
        await _channel.Writer.WriteAsync(message);
    }

    /// <summary>
    /// 从队列中读取所有消息 (用于后台服务消费)
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步消息迭代器</returns>
    public virtual IAsyncEnumerable<LikeMessage> DequeueAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
