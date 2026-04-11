using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Exceptions;
using MarketOurs.DataAPI.Services;
using MarketOurs.WebAPI.Controllers;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketOurs.Test.Controllers;

[TestFixture]
public class CommentControllerTests : ControllerTestBase
{
    private Mock<ICommentService> _mockCommentService;
    private Mock<ILogger<CommentController>> _mockLogger;
    private CommentController _controller;

    [SetUp]
    public void Setup()
    {
        _mockCommentService = new Mock<ICommentService>();
        _mockLogger = new Mock<ILogger<CommentController>>();
        _controller = new CommentController(_mockCommentService.Object, _mockLogger.Object);
        SetupUser(_controller, "user_1");
    }

    [Test]
    public async Task GetAll_ShouldReturnComments()
    {
        // Arrange
        var comments = new List<CommentDto> { new CommentDto { Id = "1", Content = "Comment 1" } };
        var pagedResult = PagedResultDto<CommentDto>.Success(comments, 1, 1, 10);
        _mockCommentService.Setup(s => s.GetAllAsync(It.IsAny<PaginationParams>())).ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(new PaginationParams());

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Code, Is.EqualTo(200));
        Assert.That(result.Data!.Items.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task Create_ShouldAssignUserIdAndReturnCreatedComment()
    {
        // Arrange
        var createDto = new CommentCreateDto { PostId = "post_1", Content = "New Comment" };
        var createdComment = new CommentDto { Id = "comment_1", UserId = "user_1", Content = "New Comment" };
        _mockCommentService.Setup(s => s.CreateAsync(It.IsAny<CommentCreateDto>())).ReturnsAsync(createdComment);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Code, Is.EqualTo(200));
        _mockCommentService.Verify(s => s.CreateAsync(It.Is<CommentCreateDto>(d => d.UserId == "user_1")), Times.Once);
    }

    [Test]
    public async Task Delete_WhenIsAuthor_ShouldAllowDeletion()
    {
        // Arrange
        var comment = new CommentDto { Id = "1", UserId = "user_1" };
        _mockCommentService.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(comment);
        _mockCommentService.Setup(s => s.DeleteAsync("1")).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete("1");

        // Assert
        Assert.That(result.Code, Is.EqualTo(200));
        _mockCommentService.Verify(s => s.DeleteAsync("1"), Times.Once);
    }

    [Test]
    public void Delete_WhenNotAuthorAndNotAdmin_ShouldThrowForbiddenException()
    {
        // Arrange
        var comment = new CommentDto { Id = "1", UserId = "other_user" };
        _mockCommentService.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(comment);

        // Act
        var ex = Assert.ThrowsAsync<AuthException>(async () => await _controller.Delete("1"));

        // Assert
        Assert.That(ex!.ErrorCode, Is.EqualTo(ErrorCode.CommentDeleteDenied));
        Assert.That(ex.HttpStatusCode, Is.EqualTo(403));
        _mockCommentService.Verify(s => s.DeleteAsync("1"), Times.Never);
    }
}
