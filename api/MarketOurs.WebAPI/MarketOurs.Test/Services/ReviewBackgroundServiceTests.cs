using MarketOurs.Data.DataModels;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services;
using MarketOurs.DataAPI.Services.Background;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketOurs.Test.Services;

[TestFixture]
public class ReviewBackgroundServiceTests
{
    [Test]
    public async Task ExecuteAsync_WhenReviewingComment_ShouldUpdateCommentReviewOnly()
    {
        var queue = new ReviewMessageQueue();
        var notificationQueue = new NotificationMessageQueue();
        using var memoryCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 });
        var distributedCache = new Mock<IDistributedCache>();
        var logger = new Mock<ILogger<ReviewBackgroundService>>();

        var postRepo = new Mock<IPostRepo>();
        var commentRepo = new Mock<ICommentRepo>();
        var reviewService = new Mock<IReviewService>();

        commentRepo.Setup(r => r.GetByIdAsync("comment_1")).ReturnsAsync(new CommentModel
        {
            Id = "comment_1",
            PostId = "post_1",
            UserId = "user_1",
            Content = "test"
        });
        commentRepo.Setup(r => r.SetReviewStatusAsync("comment_1", true)).Returns(Task.CompletedTask);
        reviewService.Setup(r => r.Review(It.IsAny<string>())).ReturnsAsync(string.Empty);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(sp => sp.GetService(typeof(IPostRepo))).Returns(postRepo.Object);
        serviceProvider.Setup(sp => sp.GetService(typeof(ICommentRepo))).Returns(commentRepo.Object);
        serviceProvider.Setup(sp => sp.GetService(typeof(IReviewService))).Returns(reviewService.Object);

        var scope = new Mock<IServiceScope>();
        scope.SetupGet(s => s.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(factory => factory.CreateScope()).Returns(scope.Object);

        var service = new TestableReviewBackgroundService(
            queue,
            scopeFactory.Object,
            notificationQueue,
            memoryCache,
            distributedCache.Object,
            logger.Object);

        var cts = new CancellationTokenSource();
        var runTask = service.RunAsync(cts.Token);
        await queue.EnqueueAsync(new ReviewMessage("comment_1", ReviewType.Comment));

        await Task.Delay(100);
        cts.Cancel();
        try
        {
            await runTask;
        }
        catch (OperationCanceledException)
        {
        }

        commentRepo.Verify(r => r.SetReviewStatusAsync("comment_1", true), Times.Once);
        postRepo.Verify(r => r.SetReviewStatusAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    private sealed class TestableReviewBackgroundService(
        ReviewMessageQueue queue,
        IServiceScopeFactory scopeFactory,
        NotificationMessageQueue notificationQueue,
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ILogger<ReviewBackgroundService> logger)
        : ReviewBackgroundService(queue, scopeFactory, notificationQueue, memoryCache, distributedCache, logger)
    {
        public Task RunAsync(CancellationToken cancellationToken) => ExecuteAsync(cancellationToken);
    }
}
