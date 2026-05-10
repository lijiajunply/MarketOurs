using MarketOurs.Data.DataModels;
using MarketOurs.DataAPI.Configs;
using MarketOurs.DataAPI.Exceptions;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services;
using MarketOurs.DataAPI.Services.Background;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace MarketOurs.Test.Services;

[TestFixture]
public class LikeManagerTests
{
    private Mock<IConnectionMultiplexer> _mockRedis;
    private Mock<IDatabase> _mockDatabase;
    private Mock<LikeMessageQueue> _mockQueue;
    private Mock<IPostRepo> _mockPostRepo;
    private Mock<ICommentRepo> _mockCommentRepo;
    private Mock<ILockService> _mockLockService;
    private Mock<ILogger<LikeManager>> _mockLogger;
    private LikeManager _likeManager;

    [SetUp]
    public void Setup()
    {
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockDatabase = new Mock<IDatabase>();
        _mockQueue = new Mock<LikeMessageQueue>();
        _mockPostRepo = new Mock<IPostRepo>();
        _mockCommentRepo = new Mock<ICommentRepo>();
        _mockLockService = new Mock<ILockService>();
        _mockLogger = new Mock<ILogger<LikeManager>>();

        _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockDatabase.Object);
        var redisList = new List<IConnectionMultiplexer> { _mockRedis.Object };

        // Default lock success
        _mockLockService.Setup(l => l.AcquireAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);

        _likeManager = new LikeManager(
            redisList,
            _mockQueue.Object,
            _mockPostRepo.Object,
            _mockCommentRepo.Object,
            _mockLockService.Object,
            _mockLogger.Object
        );
    }

    /// <summary>
    /// 创建模拟 Lua 脚本返回值（通过反射访问 RedisResult 内部构造函数）。
    /// [isMember, primaryCount, oppositeCount, oppositeRemoved]
    /// </summary>
    private static RedisResult CreateToggleResult(int isMember, int primaryCount, int oppositeCount, int oppositeRemoved)
    {
        RedisValue[] raw = { isMember, primaryCount, oppositeCount, oppositeRemoved };
        return RedisResult.Create(raw);
    }

    [Test]
    public async Task GetPostLikesAsync_WhenKeyExists_ShouldReturnRedisCount()
    {
        // Arrange
        var postId = "post1";
        var key = CacheKeys.PostLikes(postId);
        _mockDatabase.Setup(db => db.KeyExistsAsync(key, It.IsAny<CommandFlags>())).ReturnsAsync(true);
        _mockDatabase.Setup(db => db.SetLengthAsync(key, It.IsAny<CommandFlags>())).ReturnsAsync(42);

        // Act
        var result = await _likeManager.GetPostLikesAsync(postId, 10);

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public async Task GetPostLikesAsync_WhenKeyDoesNotExist_ShouldReturnFallback()
    {
        // Arrange
        var postId = "post1";
        var key = CacheKeys.PostLikes(postId);
        _mockDatabase.Setup(db => db.KeyExistsAsync(key, It.IsAny<CommandFlags>())).ReturnsAsync(false);

        // Act
        var result = await _likeManager.GetPostLikesAsync(postId, 10);

        // Assert
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public async Task SetPostLikeAsync_WhenNotLiked_ShouldAddLikeAndEnqueueMessage()
    {
        // Arrange
        var postId = "post1";
        var userId = "user1";
        var likeKey = CacheKeys.PostLikes(postId);
        var dislikeKey = CacheKeys.PostDislikes(postId);

        _mockDatabase.Setup(db => db.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
        // Lua 脚本返回: isMember=1 (已添加), likeCount=5, dislikeCount=2, oppositeRemoved=0
        _mockDatabase.Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(CreateToggleResult(1, 5, 2, 0));

        // Act
        var result = await _likeManager.SetPostLikeAsync(postId, userId);

        // Assert
        Assert.That(result.IsLiked, Is.True);
        Assert.That(result.IsDisliked, Is.False);
        Assert.That(result.LikeCount, Is.EqualTo(5));
        Assert.That(result.DislikeCount, Is.EqualTo(2));
        _mockQueue.Verify(q => q.EnqueueAsync(It.Is<LikeMessage>(m =>
            m.Target == TargetType.Post && m.Action == ActionType.Like && m.TargetId == postId && m.UserId == userId)), Times.Once);
    }

    [Test]
    public async Task SetPostLikeAsync_WhenAlreadyLiked_ShouldRemoveLikeAndEnqueueUnlikeMessage()
    {
        // Arrange
        var postId = "post1";
        var userId = "user1";
        var likeKey = CacheKeys.PostLikes(postId);

        _mockDatabase.Setup(db => db.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
        // Lua 脚本返回: isMember=0 (已移除), likeCount=4, dislikeCount=2, oppositeRemoved=0
        _mockDatabase.Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(CreateToggleResult(0, 4, 2, 0));

        // Act
        var result = await _likeManager.SetPostLikeAsync(postId, userId);

        // Assert
        Assert.That(result.IsLiked, Is.False);
        Assert.That(result.IsDisliked, Is.False);
        Assert.That(result.LikeCount, Is.EqualTo(4));
        Assert.That(result.DislikeCount, Is.EqualTo(2));
        _mockQueue.Verify(q => q.EnqueueAsync(It.Is<LikeMessage>(m =>
            m.Action == ActionType.Unlike)), Times.Once);
        _mockQueue.Verify(q => q.EnqueueAsync(It.Is<LikeMessage>(m =>
            m.Action == ActionType.Like)), Times.Never);
    }

    [Test]
    public async Task SetPostLikeAsync_WhenAlreadyDisliked_ShouldRemoveDislikeAndAddLike()
    {
        // Arrange
        var postId = "post1";
        var userId = "user1";
        var likeKey = CacheKeys.PostLikes(postId);
        var dislikeKey = CacheKeys.PostDislikes(postId);

        _mockDatabase.Setup(db => db.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
        // Lua 脚本返回: isMember=1 (已添加), likeCount=5, dislikeCount=1, oppositeRemoved=1 (从点踩中移除)
        _mockDatabase.Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(CreateToggleResult(1, 5, 1, 1));

        // Act
        var result = await _likeManager.SetPostLikeAsync(postId, userId);

        // Assert
        Assert.That(result.IsLiked, Is.True);
        Assert.That(result.IsDisliked, Is.False);
        Assert.That(result.LikeCount, Is.EqualTo(5));
        Assert.That(result.DislikeCount, Is.EqualTo(1));
        // 应该入队两条消息：取消点踩 + 点赞
        _mockQueue.Verify(q => q.EnqueueAsync(It.Is<LikeMessage>(m => m.Action == ActionType.Undislike)), Times.Once);
        _mockQueue.Verify(q => q.EnqueueAsync(It.Is<LikeMessage>(m => m.Action == ActionType.Like)), Times.Once);
    }

    [Test]
    public async Task SetPostLikeAsync_WhenLockAcquisitionFails_ShouldThrowException()
    {
        // Arrange
        _mockLockService.Setup(l => l.AcquireAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(false);

        // Act & Assert
        var ex = Assert.ThrowsAsync<ResourceAccessException>(() => _likeManager.SetPostLikeAsync("post1", "user1"));
        Assert.That(ex.Message, Does.Contain("操作过于频繁"));
        _mockQueue.Verify(q => q.EnqueueAsync(It.IsAny<LikeMessage>()), Times.Never);
    }

    [Test]
    public async Task SetPostLikeAsync_WhenCacheMissing_ShouldFetchFromDb()
    {
        // Arrange
        var postId = "post1";
        var userId = "user1";
        var likeKey = CacheKeys.PostLikes(postId);

        _mockDatabase.Setup(db => db.KeyExistsAsync(likeKey, It.IsAny<CommandFlags>())).ReturnsAsync(false);
        _mockDatabase.Setup(db => db.KeyExistsAsync(CacheKeys.PostDislikes(postId), It.IsAny<CommandFlags>())).ReturnsAsync(true);
        // Lua 脚本返回
        _mockDatabase.Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(CreateToggleResult(1, 5, 2, 0));

        var dbUsers = new List<UserModel> { new() { Id = "user2" } };
        _mockPostRepo.Setup(r => r.GetLikeUsersAsync(postId)).ReturnsAsync(dbUsers);

        // Act
        var result = await _likeManager.SetPostLikeAsync(postId, userId);

        // Assert
        Assert.That(result.IsLiked, Is.True);
        Assert.That(result.LikeCount, Is.EqualTo(5));
        _mockPostRepo.Verify(r => r.GetLikeUsersAsync(postId), Times.Once);
        _mockDatabase.Verify(db => db.SetAddAsync(likeKey, It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()), Times.Once);
    }
}
