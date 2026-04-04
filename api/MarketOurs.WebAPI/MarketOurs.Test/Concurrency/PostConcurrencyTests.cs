using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace MarketOurs.Test.Concurrency;

[TestFixture]
[Category("Concurrency")]
public class PostConcurrencyTests
{
    private Mock<IPostRepo> _mockPostRepo;
    private Mock<IUserRepo> _mockUserRepo;
    private Mock<ILikeManager> _mockLikeManager;
    private Mock<IDistributedCache> _mockDistributedCache;
    private Mock<IMemoryCache> _mockMemoryCache;
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
        _mockMemoryCache = new Mock<IMemoryCache>();
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockDatabase = new Mock<IDatabase>();
        _mockLogger = new Mock<ILogger<PostService>>();

        _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockDatabase.Object);
        var redisList = new List<IConnectionMultiplexer> { _mockRedis.Object };

        // Setup MemoryCache mock
        object? expectedValue = null;
        _mockMemoryCache
            .Setup(m => m.TryGetValue(It.IsAny<object>(), out expectedValue))
            .Returns(false);
        _mockMemoryCache
            .Setup(m => m.CreateEntry(It.IsAny<object>()))
            .Returns(new Mock<ICacheEntry>().Object);

        _postService = new PostService(
            _mockPostRepo.Object,
            _mockUserRepo.Object,
            _mockLikeManager.Object,
            _mockDistributedCache.Object,
            _mockMemoryCache.Object,
            redisList,
            _mockLogger.Object
        );
    }

    [Test]
    public async Task IncrementWatchAsync_ConcurrentCalls_TriggersSyncCorrectNumberOfTimes()
    {
        // Arrange
        const string postId = "test-post";
        const int totalIncrements = 100; // Total calls
        const int threshold = 10; // From PostService.cs: WatchSyncThreshold = 10
        const int expectedSyncs = totalIncrements / threshold; // 10 syncs expected

        // Use a counter to track Redis increments (thread-safe)
        long redisCounter = 0;
        _mockDatabase.Setup(db => db.StringIncrementAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<CommandFlags>()))
            .Returns(() => Task.FromResult(Interlocked.Increment(ref redisCounter)));

        // ManualResetEvent or Semaphore to wait for background tasks to finish (since they use Task.Run)
        // This is tricky because Task.Run is detached. In a real test, we might add a delay or use a more sophisticated way to track completion.
        int actualSyncCalls = 0;
        var syncFinishedEvent = new CountdownEvent(expectedSyncs);

        _mockPostRepo.Setup(r => r.AddWatchCountAsync(postId, threshold))
            .Returns(Task.CompletedTask)
            .Callback(() => 
            {
                Interlocked.Increment(ref actualSyncCalls);
                syncFinishedEvent.Signal();
            });

        // Act
        var tasks = new List<Task>();
        for (int i = 0; i < totalIncrements; i++)
        {
            tasks.Add(_postService.IncrementWatchAsync(postId));
        }
        await Task.WhenAll(tasks);

        // Wait for background sync tasks to finish (with a timeout)
        bool finished = syncFinishedEvent.Wait(TimeSpan.FromSeconds(5));

        // Assert
        Assert.That(finished, Is.True, "Background sync tasks did not complete in time.");
        Assert.That(actualSyncCalls, Is.EqualTo(expectedSyncs), $"AddWatchCountAsync was called {actualSyncCalls} times instead of {expectedSyncs}.");
        _mockDatabase.Verify(db => db.StringIncrementAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<CommandFlags>()), Times.Exactly(totalIncrements));
    }
}