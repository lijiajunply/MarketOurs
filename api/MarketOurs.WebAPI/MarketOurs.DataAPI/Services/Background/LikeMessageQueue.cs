using System.Threading.Channels;

namespace MarketOurs.DataAPI.Services.Background;

public enum TargetType
{
    Post,
    Comment
}

public enum ActionType
{
    Like,
    Dislike,
    Unlike,
    Undislike
}

public record LikeMessage(TargetType Target, ActionType Action, string TargetId, string UserId);

public class LikeMessageQueue
{
    private readonly Channel<LikeMessage> _channel = Channel.CreateUnbounded<LikeMessage>();

    /// <summary>
    /// 写入队列
    /// </summary>
    /// <param name="message"></param>
    public async ValueTask EnqueueAsync(LikeMessage message)
    {
        await _channel.Writer.WriteAsync(message);
    }

    /// <summary>
    /// 读队列
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public IAsyncEnumerable<LikeMessage> DequeueAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
