using MarketOurs.Data.DataModels;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketOurs.Test.Stress;

[TestFixture]
[Category("HighLoad")]
public class BulkCommentStressTests
{
    private Mock<ICommentRepo> _mockCommentRepo;
    private Mock<IUserRepo> _mockUserRepo;
    private Mock<IPostRepo> _mockPostRepo;
    private Mock<ILikeManager> _mockLikeManager;
    private Mock<IMemoryCache> _mockMemoryCache;
    private Mock<IDistributedCache> _mockDistributedCache;
    private Mock<MarketOurs.DataAPI.Services.Background.NotificationMessageQueue> _mockNotificationQueue;
    private Mock<ILogger<CommentService>> _mockLogger;
    private CommentService _commentService;

    [SetUp]
    public void Setup()
    {
        _mockCommentRepo = new Mock<ICommentRepo>();
        _mockUserRepo = new Mock<IUserRepo>();
        _mockPostRepo = new Mock<IPostRepo>();
        _mockLikeManager = new Mock<ILikeManager>();
        _mockMemoryCache = new Mock<IMemoryCache>();
        _mockDistributedCache = new Mock<IDistributedCache>();
        _mockNotificationQueue = new Mock<MarketOurs.DataAPI.Services.Background.NotificationMessageQueue>();
        _mockLogger = new Mock<ILogger<CommentService>>();

        // Basic MemoryCache mock setups
        _mockMemoryCache.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(new Mock<ICacheEntry>().Object);

        _commentService = new CommentService(
            _mockCommentRepo.Object,
            _mockUserRepo.Object,
            _mockPostRepo.Object,
            _mockLikeManager.Object,
            _mockMemoryCache.Object,
            _mockDistributedCache.Object,
            _mockNotificationQueue.Object,
            _mockLogger.Object
        );
    }

    [Test]
    public async Task CreateAsync_BulkConcurrentWrites_ShouldHandle10000Comments()
    {
        // Arrange
        const int totalComments = 10000;
        var user = new UserModel { Id = "user_1" };
        var post = new PostModel { Id = "post_1", UserId = "post_owner" };
        _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockPostRepo.Setup(r => r.GetByIdAsync("post_1")).ReturnsAsync(post);
        
        int dbInsertCount = 0;
        _mockCommentRepo.Setup(r => r.CreateAsync(It.IsAny<CommentModel>()))
            .Returns(Task.CompletedTask)
            .Callback(() => Interlocked.Increment(ref dbInsertCount));

        var createDtos = Enumerable.Range(0, totalComments).Select(i => new CommentCreateDto
        {
            UserId = "user_1",
            PostId = "post_1",
            Content = $"Comment {i}"
        }).ToList();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        // Use a higher degree of parallelism to stress the service layer
        await Parallel.ForEachAsync(createDtos, new ParallelOptions { MaxDegreeOfParallelism = 100 }, async (dto, _) =>
        {
            await _commentService.CreateAsync(dto);
        });
        stopwatch.Stop();

        // Assert
        await TestContext.Out.WriteLineAsync($"Bulk Comment Creation: {totalComments} comments in {stopwatch.ElapsedMilliseconds}ms");
        await TestContext.Out.WriteLineAsync($"Throughput: {totalComments / stopwatch.Elapsed.TotalSeconds:F2} comments/s");
        
        Assert.That(dbInsertCount, Is.EqualTo(totalComments), "Not all comments were sent to the repository!");
        _mockMemoryCache.Verify(m => m.Remove(It.IsAny<object>()), Times.AtLeast(totalComments));
    }
}
