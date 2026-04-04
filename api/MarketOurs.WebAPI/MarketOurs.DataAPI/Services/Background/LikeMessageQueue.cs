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
    Dislike
}

public record LikeMessage(TargetType Target, ActionType Action, string TargetId, string UserId);

public class LikeMessageQueue
{
    private readonly Channel<LikeMessage> _channel;

    public LikeMessageQueue()
    {
        // Unbounded channel to allow fast enqueue without blocking
        _channel = Channel.CreateUnbounded<LikeMessage>();
    }

    public async ValueTask EnqueueAsync(LikeMessage message)
    {
        await _channel.Writer.WriteAsync(message);
    }

    public IAsyncEnumerable<LikeMessage> DequeueAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
