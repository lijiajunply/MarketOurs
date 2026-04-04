using MarketOurs.Data.DataModels;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services.Background;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketOurs.Test.Services;

[TestFixture]
public class LikeSyncErrorRecoveryTests
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
    public async Task ExecuteAsync_WhenDatabaseFails_ShouldLogAndContinueToNextMessage()
    {
        // Arrange
        var userId = "user1";
        var user = new UserModel { Id = userId };
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        // 第一条消息会导致数据库异常
        var failPostId = "fail_post";
        _mockPostRepo.Setup(r => r.SetLikesAsync(user, failPostId)).ThrowsAsync(new Exception("Database connection failed"));

        // 第二条消息应该是正常的
        var successPostId = "success_post";
        bool secondMessageProcessed = false;
        _mockPostRepo.Setup(r => r.SetLikesAsync(user, successPostId))
            .Callback(() => secondMessageProcessed = true)
            .Returns(Task.CompletedTask);

        await _queue.EnqueueAsync(new LikeMessage(TargetType.Post, ActionType.Like, failPostId, userId));
        await _queue.EnqueueAsync(new LikeMessage(TargetType.Post, ActionType.Like, successPostId, userId));

        using var cts = new CancellationTokenSource();
        
        // Act
        var runTask = _service.StartAsync(cts.Token);

        // 等待足够的时间处理两条消息
        await Task.Delay(200, cts.Token);
        await _service.StopAsync(CancellationToken.None);

        // Assert
        // 验证错误被记录
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error occurred executing like/dislike sync logic")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // 验证即使第一条失败，第二条也成功处理了
        Assert.That(secondMessageProcessed, Is.True);
        _mockPostRepo.Verify(r => r.SetLikesAsync(user, successPostId), Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_WhenUserNotFound_ShouldLogWarningAndContinue()
    {
        // Arrange
        var userId = "non_existent_user";
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((UserModel?)null);

        var postId = "post1";
        await _queue.EnqueueAsync(new LikeMessage(TargetType.Post, ActionType.Like, postId, userId));

        // 后续有一条正常消息
        var validUserId = "valid_user";
        var validUser = new UserModel { Id = validUserId };
        _mockUserRepo.Setup(r => r.GetByIdAsync(validUserId)).ReturnsAsync(validUser);
        await _queue.EnqueueAsync(new LikeMessage(TargetType.Post, ActionType.Like, "post2", validUserId));

        using var cts = new CancellationTokenSource();
        
        // Act
        var runTask = _service.StartAsync(cts.Token);

        await Task.Delay(200, cts.Token);
        await _service.StopAsync(CancellationToken.None);

        // Assert
        // 验证警告被记录
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"User {userId} not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);

        // 验证正常消息被处理
        _mockPostRepo.Verify(r => r.SetLikesAsync(validUser, "post2"), Times.Once);
    }
}