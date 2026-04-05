using MarketOurs.Data;
using MarketOurs.Data.DataModels;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Configs;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services;
using MarketOurs.DataAPI.Services.Background;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;

namespace MarketOurs.Test.Integration;

/// <summary>
/// Tests business-level authorization rules enforced by the service layer.
/// Controller-level HTTP auth (401/403) is tested in controller unit tests.
/// </summary>
[TestFixture]
[Category("Integration")]
public class AuthorizationIntegrationTests : IntegrationTestBase
{
    private ServiceProvider _serviceProvider;
    private IPostService _postService;
    private ICommentService _commentService;
    private ILoginService _loginService;
    private IUserService _userService;
    private IPostRepo _postRepo;
    private IUserRepo _userRepo;
    private ICommentRepo _commentRepo;
    private IConnectionMultiplexer _redis;

    [SetUp]
    public async Task Setup()
    {
        var services = new ServiceCollection();

        var options = new DbContextOptionsBuilder<MarketContext>()
            .UseNpgsql(TestAssemblySetup.DbConnectionString)
            .Options;
        services.AddSingleton<IDbContextFactory<MarketContext>>(new TestDbContextFactory(options));
        services.AddScoped<IUserRepo, UserRepo>();
        services.AddScoped<IPostRepo, PostRepo>();
        services.AddScoped<ICommentRepo, CommentRepo>();

        _redis = CreateRedisConnection();
        _redis.GetDatabase().Execute("FLUSHDB");
        services.AddSingleton(_redis);
        services.AddSingleton<IEnumerable<IConnectionMultiplexer>>([_redis]);
        services.AddScoped<ILockService, RedisLockService>();
        services.AddSingleton<LikeMessageQueue>();
        services.AddScoped<ILikeManager, LikeManager>();
        services.AddMemoryCache(o => o.SizeLimit = 1000);
        services.AddStackExchangeRedisCache(o => o.Configuration = TestAssemblySetup.RedisConnectionString);
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        var mockEmail = new Mock<IEmailService>();
        services.AddSingleton(mockEmail.Object);

        var mockJwt = new Mock<IJwtService>();
        mockJwt.Setup(x => x.GetAccessToken(It.IsAny<UserDto>(), It.IsAny<DeviceType>()))
            .ReturnsAsync("access-token");
        mockJwt.Setup(x => x.GetRefreshToken(It.IsAny<DeviceType>()))
            .ReturnsAsync("refresh-token");
        services.AddSingleton(mockJwt.Object);

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<ILoginService, LoginService>();

        _serviceProvider = services.BuildServiceProvider();
        _postService = _serviceProvider.GetRequiredService<IPostService>();
        _commentService = _serviceProvider.GetRequiredService<ICommentService>();
        _loginService = _serviceProvider.GetRequiredService<ILoginService>();
        _userService = _serviceProvider.GetRequiredService<IUserService>();
        _postRepo = _serviceProvider.GetRequiredService<IPostRepo>();
        _userRepo = _serviceProvider.GetRequiredService<IUserRepo>();
        _commentRepo = _serviceProvider.GetRequiredService<ICommentRepo>();

        using var scope = _serviceProvider.CreateScope();
        var ctx = await scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<MarketContext>>().CreateDbContextAsync();
        ctx.Database.ExecuteSqlRaw("TRUNCATE TABLE \"users\" CASCADE");
        ctx.Database.ExecuteSqlRaw("TRUNCATE TABLE \"posts\" CASCADE");
        ctx.Database.ExecuteSqlRaw("TRUNCATE TABLE \"comments\" CASCADE");
    }

    [TearDown]
    public async Task TearDown()
    {
        await _serviceProvider.DisposeAsync();
        _redis.Dispose();
    }

    // ── Helper ──────────────────────────────────────────────────────────────────

    private async Task<UserModel> SeedUserAsync(string role = "User", string email = "user@test.com")
    {
        var user = new UserModel { Name = "Test", Email = email, Password = "hash".StringToHash(), Role = role, IsActive = true };
        await _userRepo.CreateAsync(user);
        return user;
    }

    private async Task<PostModel> SeedPostAsync(string userId)
    {
        var post = new PostModel { Title = "Test Post", Content = "Content", UserId = userId };
        await _postRepo.CreateAsync(post);
        return post;
    }

    // ── Post Ownership ────────────────────────────────────────────────────────

    [Test]
    public async Task UpdatePost_ByAuthor_Succeeds()
    {
        var user = await SeedUserAsync(email: "author@test.com");
        var post = await SeedPostAsync(user.Id);

        var result = await _postService.UpdateAsync(post.Id, new PostUpdateDto
            { Title = "Updated", Content = "New Content" });

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Title, Is.EqualTo("Updated"));
    }

    [Test]
    public async Task UpdatePost_ByNonExistentId_ReturnsNull()
    {
        // Service returns null for non-existent post — callers (controllers) enforce 403
        var result = await _postService.UpdateAsync("non-existent-post-id", new PostUpdateDto
            { Title = "Hack", Content = "Hack" });
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task DeletePost_ByNonExistentId_DoesNotThrow()
    {
        // Service should not throw when deleting a post that doesn't exist
        Assert.DoesNotThrowAsync(() => _postService.DeleteAsync("non-existent-id"));
    }

    // ── Comment Ownership ────────────────────────────────────────────────────

    [Test]
    public async Task DeleteComment_ByAuthor_RemovesComment()
    {
        var user = await SeedUserAsync(email: "commentauthor@test.com");
        var post = await SeedPostAsync(user.Id);

        var comment = await _commentService.CreateAsync(new CommentCreateDto
            { Content = "My comment", UserId = user.Id, PostId = post.Id });
        Assert.That(comment, Is.Not.Null);

        await _commentService.DeleteAsync(comment!.Id);

        var deleted = await _commentRepo.GetByIdAsync(comment.Id);
        Assert.That(deleted, Is.Null, "Comment should be deleted from DB");
    }

    [Test]
    public async Task CreateComment_WithNonExistentUser_ReturnsNull()
    {
        var user = await SeedUserAsync(email: "postowner@test.com");
        var post = await SeedPostAsync(user.Id);

        var result = await _commentService.CreateAsync(new CommentCreateDto
        {
            Content = "Injected comment",
            UserId = "non-existent-user-id",
            PostId = post.Id
        });

        Assert.That(result, Is.Null, "Comment creation should fail for non-existent user");
    }

    // ── Locked Account ────────────────────────────────────────────────────────

    [Test]
    public async Task Login_LockedUser_ThrowsAuthException()
    {
        var user = new UserModel
        {
            Name = "Locked",
            Email = "locked@test.com",
            Password = "Pass123".StringToHash(),
            IsActive = false, // locked
            Role = "User"
        };
        await _userRepo.CreateAsync(user);

        Assert.ThrowsAsync<MarketOurs.DataAPI.Exceptions.AuthException>(
            () => _userService.LoginAsync("locked@test.com", "Pass123"));
    }

    // ── Token Validation ──────────────────────────────────────────────────────

    [Test]
    public async Task ValidateToken_AfterLogout_ReturnsFalse()
    {
        var user = await SeedUserAsync(email: "logout@test.com");
        var tokenDto = await _loginService.Login(user.Email, "hash", "Web");

        // Validate succeeds before logout
        var valid = await _loginService.ValidateToken(user.Id, tokenDto.AccessToken, "Web");
        Assert.That(valid, Is.True);

        // Logout removes the token from Redis
        await _loginService.Logout(user.Id, "Web");

        // Validate should now fail
        var afterLogout = await _loginService.ValidateToken(user.Id, tokenDto.AccessToken, "Web");
        Assert.That(afterLogout, Is.False, "Token should be invalidated after logout");
    }

    [Test]
    public async Task Login_DifferentDevices_EachHasSeparateToken()
    {
        var user = await SeedUserAsync(email: "multidevice@test.com");

        var webToken = await _loginService.Login(user.Email, "hash", "Web");
        var mobileToken = await _loginService.Login(user.Email, "hash", "Mobile");

        // Each device type should independently validate
        var webValid = await _loginService.ValidateToken(user.Id, webToken.AccessToken, "Web");
        var mobileValid = await _loginService.ValidateToken(user.Id, mobileToken.AccessToken, "Mobile");

        Assert.That(webValid, Is.True);
        Assert.That(mobileValid, Is.True);

        // Logging out Web should not affect Mobile
        await _loginService.Logout(user.Id, "Web");
        var webAfter = await _loginService.ValidateToken(user.Id, webToken.AccessToken, "Web");
        var mobileAfter = await _loginService.ValidateToken(user.Id, mobileToken.AccessToken, "Mobile");

        Assert.That(webAfter, Is.False, "Web session should be logged out");
        Assert.That(mobileAfter, Is.True, "Mobile session should remain active");
    }

    private class TestDbContextFactory(DbContextOptions<MarketContext> options) : IDbContextFactory<MarketContext>
    {
        public MarketContext CreateDbContext() => new MarketContext(options);
    }
}
