using MarketOurs.Data.DataModels;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace MarketOurs.Test.Stress;

[TestFixture]
[Category("Stress")]
public class CacheAvalancheTests
{
    private Mock<IPostRepo> _mockPostRepo;
    private Mock<IUserRepo> _mockUserRepo;
    private Mock<ILikeManager> _mockLikeManager;
    private Mock<IDistributedCache> _mockDistributedCache;
    private IMemoryCache _realMemoryCache;
    private Mock<IConnectionMultiplexer> _mockRedis;
    private Mock<IDatabase> _mockDatabase;
    private Mock<ILogger<PostService>> _mockLogger;
    private PostService _postService;

    [SetUp]
    public void Setup()
    {
        _mockPostRepo = new Mock<IPostRepo>();
        _mockUserRepo = new Mock<IUserRepo>();
        _mockLikeManager = new Mock<ILikeManager>();
        _mockDistributedCache = new Mock<IDistributedCache>();
        _realMemoryCache = new MemoryCache(new MemoryCacheOptions());
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockDatabase = new Mock<IDatabase>();
        _mockLogger = new Mock<ILogger<PostService>>();

        _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockDatabase.Object);
        var redisList = new List<IConnectionMultiplexer> { _mockRedis.Object };

        _postService = new PostService(
            _mockPostRepo.Object,
            _mockUserRepo.Object,
            _mockLikeManager.Object,
            _mockDistributedCache.Object,
            _realMemoryCache,
            redisList,
            _mockLogger.Object
        );
    }

    [TearDown]
    public void TearDown()
    {
        _realMemoryCache.Dispose();
    }

    [Test]
    public async Task GetByIdAsync_ConcurrentCacheMiss_ShouldHitDbOnlyOnce()
    {
        // Arrange
        const string postId = "avalanche-test";
        var post = new PostModel { Id = postId, Title = "Title" };
        int dbCallCount = 0;

        // Mock DB with a delay to simulate high load/slow response
        _mockPostRepo.Setup(r => r.GetReviewedByIdAsync(postId))
            .Returns(async () => 
            {
                Interlocked.Increment(ref dbCallCount);
                await Task.Delay(100); // Simulate slow DB
                return post;
            });

        _mockDistributedCache.Setup(d => d.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
        _mockLikeManager.Setup(m => m.GetPostLikesAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(0);

        // Act
        // Simulate 100 concurrent requests arriving at the same time
        var tasks = new List<Task>();
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(_postService.GetByIdAsync(postId)!);
        }
        await Task.WhenAll(tasks);

        // Assert
        // With avalanche protection (Semaphore), dbCallCount should be exactly 1
        Assert.That(dbCallCount, Is.EqualTo(1), "DB was hit multiple times for the same key under concurrent load!");
    }
}
