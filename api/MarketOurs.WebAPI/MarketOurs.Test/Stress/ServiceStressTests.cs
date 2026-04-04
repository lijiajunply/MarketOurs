using System.Diagnostics;
using MarketOurs.Data.DataModels;
using MarketOurs.Data.DTOs;
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
public class ServiceStressTests
{
    private Mock<IPostRepo> _mockPostRepo;
    private Mock<IUserRepo> _mockUserRepo;
    private Mock<ILikeManager> _mockLikeManager;
    private Mock<IDistributedCache> _mockDistributedCache;
    private IMemoryCache _realMemoryCache; // Use real memory cache for better stress testing simulation
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
        _realMemoryCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 });
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
    public async Task GetHotAsync_StressTest_ValidatesCacheHitsAndThroughput()
    {
        // Arrange
        const int count = 10;
        const int totalRequests = 10000;
        var posts = new List<PostModel> { new PostModel { Id = "1", Title = "Post 1" } };
        
        // Mock DB to return data (this should only be called once if cache works)
        _mockPostRepo.Setup(r => r.GetHotAsync(count)).ReturnsAsync(posts);
        _mockLikeManager.Setup(m => m.GetPostLikesAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(0);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task<List<PostDto>>>();
        for (int i = 0; i < totalRequests; i++)
        {
            tasks.Add(_postService.GetHotAsync(count));
        }
        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        // We expect only 1 call to the repository because of MemoryCache
        _mockPostRepo.Verify(r => r.GetHotAsync(count), Times.Once);
        
        TestContext.Out.WriteLine($"Processed {totalRequests} requests in {stopwatch.ElapsedMilliseconds}ms");
        TestContext.Out.WriteLine($"Throughput: {totalRequests / (stopwatch.Elapsed.TotalSeconds):F2} req/s");
        
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(5000), "Stress test took too long.");
    }

    [Test]
    public async Task GetByIdAsync_WithMultipleKeys_StressTest()
    {
        // Arrange
        const int numUniquePosts = 100;
        const int requestsPerPost = 100;
        var postModels = Enumerable.Range(1, numUniquePosts)
            .Select(i => new PostModel { Id = i.ToString(), Title = $"Post {i}" })
            .ToList();

        _mockPostRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => postModels.FirstOrDefault(p => p.Id == id));
        _mockDistributedCache.Setup(d => d.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task>();
        for (int i = 0; i < requestsPerPost; i++)
        {
            foreach (var post in postModels)
            {
                tasks.Add(_postService.GetByIdAsync(post.Id));
            }
        }
        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        // Each unique post should hit DB once
        _mockPostRepo.Verify(r => r.GetByIdAsync(It.IsAny<string>()), Times.Exactly(numUniquePosts));
        
        TestContext.Out.WriteLine($"Processed {numUniquePosts * requestsPerPost} requests for {numUniquePosts} unique keys in {stopwatch.ElapsedMilliseconds}ms");
    }
}