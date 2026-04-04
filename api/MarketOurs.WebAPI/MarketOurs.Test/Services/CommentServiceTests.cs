using System.Collections.Generic;
using System.Threading.Tasks;
using MarketOurs.Data.DataModels;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketOurs.Test.Services;

[TestFixture]
public class CommentServiceTests
{
    private Mock<ICommentRepo> _mockCommentRepo;
    private Mock<IUserRepo> _mockUserRepo;
    private Mock<ILikeManager> _mockLikeManager;
    private Mock<IMemoryCache> _mockMemoryCache;
    private Mock<IDistributedCache> _mockDistributedCache;
    private Mock<ILogger<CommentService>> _mockLogger;
    private CommentService _commentService;

    [SetUp]
    public void Setup()
    {
        _mockCommentRepo = new Mock<ICommentRepo>();
        _mockUserRepo = new Mock<IUserRepo>();
        _mockLikeManager = new Mock<ILikeManager>();
        _mockMemoryCache = new Mock<IMemoryCache>();
        _mockDistributedCache = new Mock<IDistributedCache>();
        _mockLogger = new Mock<ILogger<CommentService>>();

        // Setup MemoryCache mock
        object? expectedValue = null;
        _mockMemoryCache
            .Setup(m => m.TryGetValue(It.IsAny<object>(), out expectedValue))
            .Returns(false);
        _mockMemoryCache
            .Setup(m => m.CreateEntry(It.IsAny<object>()))
            .Returns(new Mock<ICacheEntry>().Object);

        _commentService = new CommentService(
            _mockCommentRepo.Object,
            _mockUserRepo.Object,
            _mockLikeManager.Object,
            _mockMemoryCache.Object,
            _mockDistributedCache.Object,
            _mockLogger.Object
        );
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnCommentsWithLikes()
    {
        // Arrange
        var comments = new List<CommentModel> { new CommentModel { Id = "1", Content = "Comment 1" } };
        _mockCommentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(comments);
        _mockLikeManager.Setup(m => m.GetCommentLikesAsync("1", It.IsAny<int>())).ReturnsAsync(5);

        // Act
        var result = await _commentService.GetAllAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result[0].Likes, Is.EqualTo(5));
    }

    [Test]
    public async Task CreateAsync_WithValidUser_ShouldCreateComment()
    {
        // Arrange
        var user = new UserModel { Id = "user_1" };
        var createDto = new CommentCreateDto { UserId = "user_1", PostId = "post_1", Content = "New Comment" };
        
        _mockUserRepo.Setup(r => r.GetByIdAsync("user_1")).ReturnsAsync(user);
        CommentModel? createdComment = null;
        _mockCommentRepo.Setup(r => r.CreateAsync(It.IsAny<CommentModel>()))
            .Callback<CommentModel>(c => createdComment = c)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _commentService.CreateAsync(createDto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(createdComment, Is.Not.Null);
        Assert.That(createdComment!.Content, Is.EqualTo("New Comment"));
        _mockCommentRepo.Verify(r => r.CreateAsync(It.IsAny<CommentModel>()), Times.Once);
    }

    [Test]
    public async Task SetLikesAsync_WhenCommentExists_ShouldCallLikeManager()
    {
        // Arrange
        var comment = new CommentModel { Id = "1" };
        _mockCommentRepo.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(comment);

        // Act
        await _commentService.SetLikesAsync("user_1", "1");

        // Assert
        _mockLikeManager.Verify(m => m.SetCommentLikeAsync("1", "user_1"), Times.Once);
    }
}