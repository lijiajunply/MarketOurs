using MarketOurs.Data.DataModels;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;

namespace MarketOurs.Test.Stress;

[TestFixture]
[Category("Stress")]
public class CacheScenariosTests
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
        
        // Setup basic user and like mocks to avoid null refs
        _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(new UserModel { Id = "test-user", Name = "TestUser" });
        _mockLikeManager.Setup(m => m.GetPostLikesAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(0);
    }

    [TearDown]
    public void TearDown()
    {
        _realMemoryCache.Dispose();
    }

    /// <summary>
    /// 缓存击穿测试 (Cache Breakdown): 
    /// 模拟热点数据缓存突然失效，大量并发请求涌入时，应只有第一个请求能去查数据库，其余请求需等待其完成并复用结果。
    /// </summary>
    [Test]
    public async Task GetHotAsync_CacheBreakdown_ShouldHitDbOnlyOnce()
    {
        // Arrange
        int dbCallCount = 0;
        var hotPostsFromDb = new List<PostModel>
        {
            new() { Id = "hot-1", Title = "Hot Post 1" },
            new() { Id = "hot-2", Title = "Hot Post 2" }
        };

        // 模拟数据库延迟，放大并发查询落到DB的几率
        _mockPostRepo.Setup(r => r.GetHotAsync(It.IsAny<int>()))
            .Returns(async () =>
            {
                Interlocked.Increment(ref dbCallCount);
                await Task.Delay(100);
                return hotPostsFromDb;
            });

        // 模拟 Redis 缓存未命中
        _mockDistributedCache.Setup(d => d.GetAsync(It.IsAny<string>(), CancellationToken.None))
            .ReturnsAsync((byte[]?)null);

        // Act - 启动 100 个并发请求
        int concurrency = 100;
        var tasks = Enumerable.Range(0, concurrency)
            .Select(_ => _postService.GetHotAsync(10))
            .ToList();

        await Task.WhenAll(tasks);

        // Assert - SemaphoreSlim 应保障只有一个请求透传到了数据库
        Assert.That(dbCallCount, Is.EqualTo(1), "Cache Breakdown protection failed: DB was hit multiple times.");
        
        // 校验返回的数据是否正确
        foreach (var task in tasks)
        {
            var result = await task;
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
        }
    }

    /// <summary>
    /// 缓存雪崩测试 (Cache Avalanche) / Redis宕机:
    /// 模拟 Redis 完全宕机不可用（抛出异常），大量请求应回源数据库一次后，利用本地内存缓存顶住后续请求，保持高可用。
    /// </summary>
    [Test]
    public async Task GetHotAsync_CacheAvalanche_RedisDown_ShouldGracefullyFallbackToMemoryCache()
    {
        // Arrange
        int dbCallCount = 0;
        var hotPostsFromDb = new List<PostModel>
        {
            new() { Id = "hot-1", Title = "Hot Post 1" }
        };

        _mockPostRepo.Setup(r => r.GetHotAsync(It.IsAny<int>()))
            .Returns(async () =>
            {
                Interlocked.Increment(ref dbCallCount);
                await Task.Delay(50);
                return hotPostsFromDb;
            });

        // 模拟 Redis 抛出异常（宕机）
        _mockDistributedCache.Setup(d => d.GetAsync(It.IsAny<string>(), CancellationToken.None))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis is dead"));

        // Act - 第一个请求，应该触发异常捕获并回源DB，随后存入内存缓存
        var firstResult = await _postService.GetHotAsync(10);
        
        // 发起后续 50 个并发请求
        var tasks = Enumerable.Range(0, 50)
            .Select(_ => _postService.GetHotAsync(10))
            .ToList();
            
        await Task.WhenAll(tasks);

        // Assert
        Assert.That(firstResult.Count, Is.EqualTo(1));
        // 尽管 Redis 挂了，本地缓存应该拦截了后续所有请求，DB 调用应该仍然只有一次
        Assert.That(dbCallCount, Is.EqualTo(1), "Redis failure led to multiple DB hits (local cache fallback failed).");
    }

    /// <summary>
    /// 缓存穿透测试 (Cache Penetration):
    /// 查询一个数据库中不存在的无效ID。高并发下应防止恶意请求全部落到数据库。
    /// 业务中有本地缓存，且通过并发锁能限制同一ID的并发回源，这里验证恶意高并发请求同一个假ID是否能被拦截。
    /// </summary>
    [Test]
    public async Task GetByIdAsync_CachePenetration_ConcurrentFakeIds_ShouldHitDbOnlyOnce()
    {
        // Arrange
        const string fakePostId = "non-existent-fake-id-999";
        int dbCallCount = 0;

        // 模拟数据库返回 null
        _mockPostRepo.Setup(r => r.GetByIdAsync(fakePostId))
            .Returns(async () =>
            {
                Interlocked.Increment(ref dbCallCount);
                await Task.Delay(100);
                return null;
            });

        _mockDistributedCache.Setup(d => d.GetAsync(It.IsAny<string>(), CancellationToken.None))
            .ReturnsAsync((byte[]?)null);

        // Act - 发起 100 个并发请求
        int concurrency = 100;
        var tasks = Enumerable.Range(0, concurrency)
            .Select(_ => _postService.GetByIdAsync(fakePostId))
            .ToList();

        await Task.WhenAll(tasks);

        // Assert - 并发锁应当生效，保证即使查询不存在的数据，相同参数同时到达时只查一次DB
        // 备注：如果系统还实现了空值缓存(Cache Nulls)以彻底防范缓存穿透，后续独立请求也不会查DB。
        // 当前重点是验证即使不存空值，并发锁也能防止“穿透风暴”。
        Assert.That(dbCallCount, Is.EqualTo(1), "Cache Penetration protection failed: DB was hit multiple times for a non-existent ID concurrently.");
        
        foreach (var task in tasks)
        {
            var result = await task;
            Assert.That(result, Is.Null);
        }
    }
}