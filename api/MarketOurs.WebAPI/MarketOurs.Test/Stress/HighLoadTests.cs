using System.Diagnostics;
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
[Category("HighLoad")]
public class HighLoadTests
{
    private Mock<IPostRepo> _mockPostRepo;
    private Mock<IUserRepo> _mockUserRepo;
    private Mock<ILikeManager> _mockLikeManager;
    private Mock<IDistributedCache> _mockDistributedCache;
    private Mock<ILockService> _mockLockService;
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
        _mockLockService = new Mock<ILockService>();
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
    public async Task GetHotAsync_ExtremeThroughput_ShouldHandle100000Requests()
    {
        // Arrange
        const int totalRequests = 100000;
        var posts = new List<PostModel> { new PostModel { Id = "hot", Title = "Hot Post" } };
        _mockPostRepo.Setup(r => r.GetHotAsync(It.IsAny<int>())).ReturnsAsync(posts);
        _mockLikeManager.Setup(m => m.GetPostLikesAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(100);

        // Pre-warm cache
        await _postService.GetHotAsync(10);

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        // Use Parallel.ForEachAsync for more realistic high-concurrency scheduling
        await Parallel.ForEachAsync(Enumerable.Range(0, totalRequests), new ParallelOptions { MaxDegreeOfParallelism = 200 }, async (_, _) =>
        {
            await _postService.GetHotAsync(10);
        });
        
        stopwatch.Stop();

        // Assert
        var rps = totalRequests / stopwatch.Elapsed.TotalSeconds;
        await TestContext.Out.WriteLineAsync($"Extreme Throughput Test: {totalRequests} requests in {stopwatch.ElapsedMilliseconds}ms");
        await TestContext.Out.WriteLineAsync($"RPS: {rps:F2}");

        Assert.That(rps, Is.GreaterThan(10000), "Throughput dropped below 10,000 RPS!");
        _mockPostRepo.Verify(r => r.GetHotAsync(It.IsAny<int>()), Times.Once); // Only hit DB once
    }

    [Test]
    public async Task DistributedLock_HighConcurrencyContention_ShouldBeStable()
    {
        // Arrange
        var lockService = new RedisLockService(new List<IConnectionMultiplexer> { _mockRedis.Object });
        const int concurrentAttempts = 10000;
        int successfulLocks = 0;

        // Mock LockTakeAsync to be atomic and thread-safe
        _mockDatabase.Setup(db => db.LockTakeAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(() => Interlocked.CompareExchange(ref successfulLocks, 1, 0) == 0);

        // Act
        var tasks = new List<Task<bool>>();
        for (int i = 0; i < concurrentAttempts; i++)
        {
            tasks.Add(lockService.AcquireAsync("contention-lock", Guid.NewGuid().ToString(), TimeSpan.FromSeconds(10)));
        }
        var results = await Task.WhenAll(tasks);

        // Assert
        int winCount = results.Count(r => r == true);
        await TestContext.Out.WriteLineAsync($"Distributed Lock Contention: {winCount} winner(s) out of {concurrentAttempts} attempts");
        
        Assert.That(winCount, Is.EqualTo(1), "More than one thread acquired the distributed lock!");
    }

    [Test]
    public async Task PostService_ConcurrentWatchIncrements_ExtremeLoad()
    {
        // Arrange
        const int totalIncrements = 50000;
        const string postId = "extreme-post";
        long redisCounter = 0;
        int dbSyncCount = 0;

        _mockDatabase.Setup(db => db.StringIncrementAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<CommandFlags>()))
            .Returns(() => Task.FromResult(Interlocked.Increment(ref redisCounter)));

        var syncFinishedEvent = new CountdownEvent(totalIncrements / 10); // 10 is the threshold

        _mockPostRepo.Setup(r => r.AddWatchCountAsync(postId, 10))
            .Returns(Task.CompletedTask)
            .Callback(() => 
            {
                Interlocked.Increment(ref dbSyncCount);
                syncFinishedEvent.Signal();
            });

        // Act
        var stopwatch = Stopwatch.StartNew();
        await Parallel.ForEachAsync(Enumerable.Range(0, totalIncrements), new ParallelOptions { MaxDegreeOfParallelism = 500 }, async (_, _) =>
        {
            await _postService.IncrementWatchAsync(postId);
        });
        
        // Wait for all background syncs to finish
        bool allSynced = syncFinishedEvent.Wait(TimeSpan.FromSeconds(10));
        stopwatch.Stop();

        // Assert
        await TestContext.Out.WriteLineAsync($"Extreme Watch Increment Load: {totalIncrements} calls in {stopwatch.ElapsedMilliseconds}ms");
        Assert.That(allSynced, Is.True, $"Background syncs timed out. Completed: {dbSyncCount}/{totalIncrements/10}");
        Assert.That(dbSyncCount, Is.EqualTo(totalIncrements / 10));
    }
}