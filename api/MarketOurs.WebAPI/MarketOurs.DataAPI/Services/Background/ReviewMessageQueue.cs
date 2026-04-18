using System.Threading.Channels;

namespace MarketOurs.DataAPI.Services.Background;

public record ReviewMessage(string TargetId, ReviewType Type);

public enum ReviewType
{
    Post,
    Comment
}

public class ReviewMessageQueue
{
    private readonly Channel<ReviewMessage> _channel = Channel.CreateUnbounded<ReviewMessage>();

    public async ValueTask EnqueueAsync(ReviewMessage message)
    {
        await _channel.Writer.WriteAsync(message);
    }

    public IAsyncEnumerable<ReviewMessage> DequeueAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
