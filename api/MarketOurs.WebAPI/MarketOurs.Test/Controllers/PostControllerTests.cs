using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Services;
using MarketOurs.WebAPI.Controllers;
using Moq;

namespace MarketOurs.Test.Controllers;

[TestFixture]
public class PostControllerTests : ControllerTestBase
{
    private Mock<IPostService> _mockPostService;
    private PostController _controller;

    [SetUp]
    public void Setup()
    {
        _mockPostService = new Mock<IPostService>();
        _controller = new PostController(_mockPostService.Object);
        SetupUser(_controller, "1");
    }

    [Test]
    public async Task GetAll_ShouldReturnAllPosts()
    {
        // Arrange
        var posts = new List<PostDto> { new PostDto { Id = "1", Title = "Post 1" } };
        var pagedResult = PagedResultDto<PostDto>.Success(posts, 1, 1, 10);
        _mockPostService.Setup(s => s.GetAllAsync(It.IsAny<PaginationParams>())).ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(new PaginationParams());

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Code, Is.EqualTo(200));
        Assert.That(result.Data!.Items.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task GetByUserId_ShouldReturnPagedPosts()
    {
        var posts = new List<PostDto> { new PostDto { Id = "1", Title = "Post 1", UserId = "user-1" } };
        var pagedResult = PagedResultDto<PostDto>.Success(posts, 1, 1, 10);
        _mockPostService.Setup(s => s.GetByUserIdAsync("user-1", It.IsAny<PaginationParams>())).ReturnsAsync(pagedResult);

        var result = await _controller.GetByUserId("user-1", new PaginationParams());

        Assert.That(result.Code, Is.EqualTo(200));
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data!.Items.Count, Is.EqualTo(1));
        Assert.That(result.Data.Items[0].UserId, Is.EqualTo("user-1"));
    }

    [Test]
    public async Task GetById_WhenExists_ShouldReturnPostAndIncrementWatch()
    {
        // Arrange
        var post = new PostDto { Id = "1", Title = "Post 1" };
        _mockPostService.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(post);
        _mockPostService.Setup(s => s.IncrementWatchAsync("1")).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.GetById("1");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Code, Is.EqualTo(200));
        _mockPostService.Verify(s => s.IncrementWatchAsync("1"), Times.Once);
    }

    [Test]
    public async Task Create_AsUser_ShouldReturnCreatedPost()
    {
        // Arrange
        var createDto = new PostCreateDto { Title = "New Post" };
        var createdPost = new PostDto { Id = "1", Title = "New Post", UserId = "1" };
        _mockPostService.Setup(s => s.CreateAsync(createDto)).ReturnsAsync(createdPost);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Code, Is.EqualTo(200));
        Assert.That(result.Data!.UserId, Is.EqualTo("1"));
    }

    [Test]
    public async Task Update_WhenUserIsAuthor_ShouldReturnUpdatedPost()
    {
        // Arrange
        var updateDto = new PostUpdateDto { Title = "Updated" };
        var existingPost = new PostDto { Id = "1", UserId = "1" };
        var updatedPost = new PostDto { Id = "1", Title = "Updated", UserId = "1" };
        
        _mockPostService.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(existingPost);
        _mockPostService.Setup(s => s.UpdateAsync("1", updateDto)).ReturnsAsync(updatedPost);

        // Act
        var result = await _controller.Update("1", updateDto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Code, Is.EqualTo(200));
    }

    [Test]
    public async Task Update_WhenUserIsNotAuthorAndNotAdmin_ShouldReturn403()
    {
        // Arrange
        var updateDto = new PostUpdateDto { Title = "Updated" };
        var existingPost = new PostDto { Id = "1", UserId = "other_user" };
        _mockPostService.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(existingPost);

        // Act
        var result = await _controller.Update("1", updateDto);

        // Assert
        Assert.That(result.ErrorCode, Is.EqualTo(403));
    }

    [Test]
    public async Task Delete_AsAdmin_ShouldAllowDeletingOthersPost()
    {
        // Arrange
        SetupUser(_controller, "admin_id", "Admin");
        var existingPost = new PostDto { Id = "1", UserId = "user_id" };
        _mockPostService.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(existingPost);
        _mockPostService.Setup(s => s.DeleteAsync("1")).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete("1");

        // Assert
        Assert.That(result.Code, Is.EqualTo(200));
        _mockPostService.Verify(s => s.DeleteAsync("1"), Times.Once);
    }
}
