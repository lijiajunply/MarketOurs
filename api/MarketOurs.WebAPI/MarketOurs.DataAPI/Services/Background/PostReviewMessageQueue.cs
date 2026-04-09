using System.Threading.Channels;

namespace MarketOurs.DataAPI.Services.Background;

public record PostReviewMessage(string PostId);

public class PostReviewMessageQueue
{
    private readonly Channel<PostReviewMessage> _channel = Channel.CreateUnbounded<PostReviewMessage>();

    public async ValueTask EnqueueAsync(PostReviewMessage message)
    {
        await _channel.Writer.WriteAsync(message);
    }

    public IAsyncEnumerable<PostReviewMessage> DequeueAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
