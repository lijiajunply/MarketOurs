using System.Collections.Generic;
using System.Threading.Tasks;
using MarketOurs.Data.DataModels;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Configs;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace MarketOurs.Test.Services;

[TestFixture]
public class PostServiceTests
{
    private Mock<IPostRepo> _mockPostRepo;
    private Mock<IUserRepo> _mockUserRepo;
    private Mock<ILikeManager> _mockLikeManager;
    private Mock<IDistributedCache> _mockDistributedCache;
    private Mock<IMemoryCache> _mockMemoryCache;
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
        _mockMemoryCache = new Mock<IMemoryCache>();
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockDatabase = new Mock<IDatabase>();
        _mockLogger = new Mock<ILogger<PostService>>();

        _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockDatabase.Object);
        var redisList = new List<IConnectionMultiplexer> { _mockRedis.Object };

        // Setup MemoryCache mock
        object? expectedValue = null;
        _mockMemoryCache
            .Setup(m => m.TryGetValue(It.IsAny<object>(), out expectedValue))
            .Returns(false);
        _mockMemoryCache
            .Setup(m => m.CreateEntry(It.IsAny<object>()))
            .Returns(new Mock<ICacheEntry>().Object);

        _postService = new PostService(
            _mockPostRepo.Object,
            _mockUserRepo.Object,
            _mockLikeManager.Object,
            _mockDistributedCache.Object,
            _mockMemoryCache.Object,
            redisList,
            _mockLogger.Object
        );
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllPostsWithDynamicData()
    {
        // Arrange
        var posts = new List<PostModel>
        {
            new PostModel { Id = "1", Title = "Post 1", Content = "Content 1" },
            new PostModel { Id = "2", Title = "Post 2", Content = "Content 2" }
        };
        _mockPostRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(posts);
        
        _mockLikeManager.Setup(m => m.GetPostLikesAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(10);
        _mockLikeManager.Setup(m => m.GetPostDislikesAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(2);
        
        _mockDatabase.Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue("100"));

        // Act
        var result = await _postService.GetAllAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0].Id, Is.EqualTo("1"));
        Assert.That(result[0].Likes, Is.EqualTo(10));
        Assert.That(result[0].Dislikes, Is.EqualTo(2));
        Assert.That(result[0].Watch, Is.EqualTo(100));
    }

    [Test]
    public async Task GetByIdAsync_WhenPostExists_ShouldReturnPostDto()
    {
        // Arrange
        var post = new PostModel { Id = "1", Title = "Test Post" };
        _mockPostRepo.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(post);
        
        // Mock redis missing the distributed cache
        _mockDistributedCache.Setup(d => d.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[]?)null);

        // Act
        var result = await _postService.GetByIdAsync("1");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo("1"));
        Assert.That(result.Title, Is.EqualTo("Test Post"));
    }

    [Test]
    public async Task CreateAsync_WithValidUser_ShouldCreatePost()
    {
        // Arrange
        var createDto = new PostCreateDto { UserId = "1", Title = "New Post", Content = "New Content" };
        var user = new UserModel { Id = "1", Name = "User1" };
        _mockUserRepo.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(user);

        PostModel? createdPost = null;
        _mockPostRepo.Setup(r => r.CreateAsync(It.IsAny<PostModel>()))
            .Callback<PostModel>(p => createdPost = p)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _postService.CreateAsync(createDto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(createdPost, Is.Not.Null);
        Assert.That(createdPost!.Title, Is.EqualTo("New Post"));
        Assert.That(createdPost.Content, Is.EqualTo("New Content"));
        Assert.That(createdPost.UserId, Is.EqualTo("1"));
    }

    [Test]
    public async Task UpdateAsync_WhenPostExists_ShouldUpdatePost()
    {
        // Arrange
        var post = new PostModel { Id = "1", Title = "Old Title", Content = "Old Content" };
        var updateDto = new PostUpdateDto { Title = "New Title", Content = "New Content" };
        
        _mockPostRepo.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(post);
        _mockPostRepo.Setup(r => r.UpdateAsync(It.IsAny<PostModel>())).Returns(Task.CompletedTask);

        // Act
        var result = await _postService.UpdateAsync("1", updateDto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Title, Is.EqualTo("New Title"));
        Assert.That(result.Content, Is.EqualTo("New Content"));
        Assert.That(post.Title, Is.EqualTo("New Title"));
    }

    [Test]
    public async Task DeleteAsync_ShouldCallDeleteOnRepoAndInvalidateCache()
    {
        // Arrange
        _mockPostRepo.Setup(r => r.DeleteAsync("1")).Returns(Task.CompletedTask);

        // Act
        await _postService.DeleteAsync("1");

        // Assert
        _mockPostRepo.Verify(r => r.DeleteAsync("1"), Times.Once);
        _mockMemoryCache.Verify(m => m.Remove(It.IsAny<object>()), Times.AtLeastOnce);
    }
}