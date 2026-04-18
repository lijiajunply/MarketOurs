using MarketOurs.Data.DataModels;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services;
using MarketOurs.DataAPI.Services.Background;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketOurs.Test.Services;

[TestFixture]
public class CommentServiceTests
{
    private Mock<ICommentRepo> _mockCommentRepo = null!;
    private Mock<IUserRepo> _mockUserRepo = null!;
    private Mock<ILikeManager> _mockLikeManager = null!;
    private Mock<IMemoryCache> _mockMemoryCache = null!;
    private Mock<IDistributedCache> _mockDistributedCache = null!;
    private Mock<ILogger<CommentService>> _mockLogger = null!;
    private Mock<IPostRepo> _mockPostRepo = null!;
    private NotificationMessageQueue _notificationQueue = null!;
    private ReviewMessageQueue _reviewQueue = null!;
    private CommentService _commentService = null!;

    [SetUp]
    public void Setup()
    {
        _mockCommentRepo = new Mock<ICommentRepo>();
        _mockUserRepo = new Mock<IUserRepo>();
        _mockLikeManager = new Mock<ILikeManager>();
        _mockMemoryCache = new Mock<IMemoryCache>();
        _mockDistributedCache = new Mock<IDistributedCache>();
        _mockLogger = new Mock<ILogger<CommentService>>();
        _mockPostRepo = new Mock<IPostRepo>();
        _notificationQueue = new NotificationMessageQueue();
        _reviewQueue = new ReviewMessageQueue();

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
            _mockPostRepo.Object,
            _mockLikeManager.Object,
            _mockMemoryCache.Object,
            _mockDistributedCache.Object,
            _notificationQueue,
            _mockLogger.Object,
            _reviewQueue
        );
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnCommentsWithLikes()
    {
        var comments = new List<CommentModel> { new() { Id = "1", Content = "Comment 1" } };
        _mockCommentRepo.Setup(r => r.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>())).ReturnsAsync(comments);
        _mockCommentRepo.Setup(r => r.CountAsync(It.IsAny<bool>())).ReturnsAsync(1);
        _mockLikeManager.Setup(m => m.GetCommentLikesAsync("1", It.IsAny<int>())).ReturnsAsync(5);

        var result = await _commentService.GetAllAsync(new PaginationParams());

        Assert.That(result.Items[0].Likes, Is.EqualTo(5));
    }

    [Test]
    public async Task CreateAsync_WithValidUser_ShouldCreatePendingCommentAndEnqueueReview()
    {
        var user = new UserModel { Id = "user_1" };
        var post = new PostModel { Id = "post_1", UserId = "author_1" };
        var createDto = new CommentCreateDto { UserId = "user_1", PostId = "post_1", Content = "New Comment" };

        _mockUserRepo.Setup(r => r.GetByIdAsync("user_1")).ReturnsAsync(user);
        _mockPostRepo.Setup(r => r.GetByIdAsync("post_1")).ReturnsAsync(post);

        CommentModel? createdComment = null;
        _mockCommentRepo.Setup(r => r.CreateAsync(It.IsAny<CommentModel>()))
            .Callback<CommentModel>(comment =>
            {
                comment.Id = "comment_1";
                createdComment = comment;
            })
            .Returns(Task.CompletedTask);

        var result = await _commentService.CreateAsync(createDto);

        Assert.That(result.IsReview, Is.False);
        Assert.That(createdComment, Is.Not.Null);
        Assert.That(createdComment!.IsReview, Is.False);

        await using var enumerator = _reviewQueue.DequeueAllAsync(CancellationToken.None).GetAsyncEnumerator();
        Assert.That(await enumerator.MoveNextAsync(), Is.True);
        Assert.That(enumerator.Current.TargetId, Is.EqualTo("comment_1"));
        Assert.That(enumerator.Current.Type, Is.EqualTo(ReviewType.Comment));
    }

    [Test]
    public async Task UpdateAsync_WhenNotAdmin_ShouldResetReviewAndEnqueueReview()
    {
        var comment = new CommentModel
        {
            Id = "comment_1",
            PostId = "post_1",
            Content = "Old",
            IsReview = true
        };

        _mockCommentRepo.Setup(r => r.GetByIdAsync("comment_1")).ReturnsAsync(comment);
        _mockCommentRepo.Setup(r => r.UpdateAsync(It.IsAny<CommentModel>())).Returns(Task.CompletedTask);

        var result = await _commentService.UpdateAsync("comment_1", new CommentUpdateDto { Content = "New" }, false);

        Assert.That(result.IsReview, Is.False);
        _mockCommentRepo.Verify(r => r.UpdateAsync(It.Is<CommentModel>(c => !c.IsReview && c.Content == "New")), Times.Once);

        await using var enumerator = _reviewQueue.DequeueAllAsync(CancellationToken.None).GetAsyncEnumerator();
        Assert.That(await enumerator.MoveNextAsync(), Is.True);
        Assert.That(enumerator.Current.TargetId, Is.EqualTo("comment_1"));
    }

    [Test]
    public async Task UpdateAsync_WhenAdmin_ShouldKeepReviewedAndSkipQueue()
    {
        var comment = new CommentModel
        {
            Id = "comment_2",
            PostId = "post_1",
            Content = "Old",
            IsReview = true
        };

        _mockCommentRepo.Setup(r => r.GetByIdAsync("comment_2")).ReturnsAsync(comment);
        _mockCommentRepo.Setup(r => r.UpdateAsync(It.IsAny<CommentModel>())).Returns(Task.CompletedTask);

        var result = await _commentService.UpdateAsync("comment_2", new CommentUpdateDto { Content = "Admin Update" }, true);

        Assert.That(result.IsReview, Is.True);
        _mockCommentRepo.Verify(r => r.UpdateAsync(It.Is<CommentModel>(c => c.IsReview && c.Content == "Admin Update")), Times.Once);

        using var cts = new CancellationTokenSource(30);
        await using var enumerator = _reviewQueue.DequeueAllAsync(cts.Token).GetAsyncEnumerator();
        Assert.ThrowsAsync<OperationCanceledException>(async () => await enumerator.MoveNextAsync().AsTask());
    }

    [Test]
    public async Task SetLikesAsync_WhenCommentExists_ShouldCallLikeManager()
    {
        var comment = new CommentModel { Id = "1" };
        _mockCommentRepo.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(comment);

        await _commentService.SetLikesAsync("user_1", "1");

        _mockLikeManager.Verify(m => m.SetCommentLikeAsync("1", "user_1"), Times.Once);
    }
}
