using MarketOurs.DataAPI.Configs;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services;
using MarketOurs.DataAPI.Services.Background;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace MarketOurs.Test.Integration;

[TestFixture]
[Category("Integration")]
public class LikeManagerIntegrationTests : IntegrationTestBase
{
    private IConnectionMultiplexer _redis;
    private LikeMessageQueue _queue;
    private Mock<IPostRepo> _mockPostRepo;
    private Mock<ICommentRepo> _mockCommentRepo;
    private Mock<ILockService> _mockLockService;
    private Mock<ILogger<LikeManager>> _mockLogger;
    private LikeManager _likeManager;

    [SetUp]
    public void Setup()
    {
        _redis = CreateRedisConnection();
        // Clear redis database before each test
        _redis.GetDatabase().Execute("FLUSHDB");

        _queue = new LikeMessageQueue();
        _mockPostRepo = new Mock<IPostRepo>();
        _mockCommentRepo = new Mock<ICommentRepo>();
        _mockLockService = new Mock<ILockService>();
        _mockLogger = new Mock<ILogger<LikeManager>>();

        _mockLockService.Setup(l => l.AcquireAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);

        _likeManager = new LikeManager(
            new List<IConnectionMultiplexer> { _redis },
            _queue,
            _mockPostRepo.Object,
            _mockCommentRepo.Object,
            _mockLockService.Object,
            _mockLogger.Object
        );
    }

    [TearDown]
    public void TearDown()
    {
        _redis?.Dispose();
    }

    [Test]
    public async Task SetPostLikeAsync_Integration_ShouldToggleRedisAndEnqueue()
    {
        // Arrange
        var postId = "post123";
        var userId = "user456";
        var key = CacheKeys.PostLikes(postId);

        // Act 1: Initial Like
        await _likeManager.SetPostLikeAsync(postId, userId);

        // Assert 1: Redis should have the user
        var db = _redis.GetDatabase();
        var exists = await db.SetContainsAsync(key, userId);
        Assert.That(exists, Is.True);
        
        var length = await db.SetLengthAsync(key);
        Assert.That(length, Is.EqualTo(1));

        // Act 2: Toggle off (Unlike)
        await _likeManager.SetPostLikeAsync(postId, userId);

        // Assert 2: Redis should be empty
        exists = await db.SetContainsAsync(key, userId);
        Assert.That(exists, Is.False);
        
        length = await db.SetLengthAsync(key);
        Assert.That(length, Is.EqualTo(0));
    }

    [Test]
    public async Task SetPostLikeAsync_Integration_MutualExclusion_LikeThenDislike()
    {
        // Arrange
        var postId = "post123";
        var userId = "user456";
        var likeKey = CacheKeys.PostLikes(postId);
        var dislikeKey = CacheKeys.PostDislikes(postId);
        var db = _redis.GetDatabase();

        // Act 1: Like
        await _likeManager.SetPostLikeAsync(postId, userId);
        Assert.That(await db.SetContainsAsync(likeKey, userId), Is.True);

        // Act 2: Dislike (should remove like)
        await _likeManager.SetPostDislikeAsync(postId, userId);

        // Assert 2
        Assert.That(await db.SetContainsAsync(likeKey, userId), Is.False);
        Assert.That(await db.SetContainsAsync(dislikeKey, userId), Is.True);
    }
}