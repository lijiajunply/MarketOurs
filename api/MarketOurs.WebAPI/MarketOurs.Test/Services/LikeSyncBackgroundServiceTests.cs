using MarketOurs.Data.DataModels;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services.Background;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketOurs.Test.Services;

[TestFixture]
public class LikeSyncBackgroundServiceTests
{
    private LikeMessageQueue _queue;
    private Mock<IServiceProvider> _mockServiceProvider;
    private Mock<IServiceScopeFactory> _mockScopeFactory;
    private Mock<IServiceScope> _mockScope;
    private Mock<IUserRepo> _mockUserRepo;
    private Mock<IPostRepo> _mockPostRepo;
    private Mock<ICommentRepo> _mockCommentRepo;
    private Mock<ILogger<LikeSyncBackgroundService>> _mockLogger;
    private LikeSyncBackgroundService _service;

    [SetUp]
    public void Setup()
    {
        _queue = new LikeMessageQueue();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockScope = new Mock<IServiceScope>();
        _mockUserRepo = new Mock<IUserRepo>();
        _mockPostRepo = new Mock<IPostRepo>();
        _mockCommentRepo = new Mock<ICommentRepo>();
        _mockLogger = new Mock<ILogger<LikeSyncBackgroundService>>();

        _mockServiceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(_mockScopeFactory.Object);
        _mockScopeFactory.Setup(x => x.CreateScope()).Returns(_mockScope.Object);
        _mockScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);

        _mockServiceProvider.Setup(x => x.GetService(typeof(IUserRepo))).Returns(_mockUserRepo.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(IPostRepo))).Returns(_mockPostRepo.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(ICommentRepo))).Returns(_mockCommentRepo.Object);

        _service = new LikeSyncBackgroundService(_queue, _mockServiceProvider.Object, _mockLogger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _service.Dispose();
    }

    [Test]
    public async Task ExecuteAsync_WhenPostLikeMessageReceived_ShouldCallPostRepoSetLikes()
    {
        // Arrange
        var userId = "user1";
        var postId = "post1";
        var user = new UserModel { Id = userId };
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var message = new LikeMessage(TargetType.Post, ActionType.Like, postId, userId);
        await _queue.EnqueueAsync(message);

        using var cts = new CancellationTokenSource();
        
        // Act
        var runTask = _service.StartAsync(cts.Token);

        // Wait a bit for processing
        await Task.Delay(100);
        await _service.StopAsync(CancellationToken.None);

        // Assert
        _mockPostRepo.Verify(r => r.SetLikesAsync(user, postId), Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_WhenPostUnlikeMessageReceived_ShouldCallPostRepoDeleteLikes()
    {
        // Arrange
        var userId = "user1";
        var postId = "post1";
        var user = new UserModel { Id = userId };
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var message = new LikeMessage(TargetType.Post, ActionType.Unlike, postId, userId);
        await _queue.EnqueueAsync(message);

        using var cts = new CancellationTokenSource();
        
        // Act
        var runTask = _service.StartAsync(cts.Token);

        await Task.Delay(100);
        await _service.StopAsync(CancellationToken.None);

        // Assert
        _mockPostRepo.Verify(r => r.DeleteLikesAsync(postId, userId), Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_WhenCommentDislikeMessageReceived_ShouldCallCommentRepoSetDislikes()
    {
        // Arrange
        var userId = "user1";
        var commentId = "comment1";
        var user = new UserModel { Id = userId };
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var message = new LikeMessage(TargetType.Comment, ActionType.Dislike, commentId, userId);
        await _queue.EnqueueAsync(message);

        using var cts = new CancellationTokenSource();
        
        // Act
        var runTask = _service.StartAsync(cts.Token);

        await Task.Delay(100);
        await _service.StopAsync(CancellationToken.None);

        // Assert
        _mockCommentRepo.Verify(r => r.SetDislikesAsync(user, commentId), Times.Once);
    }
}