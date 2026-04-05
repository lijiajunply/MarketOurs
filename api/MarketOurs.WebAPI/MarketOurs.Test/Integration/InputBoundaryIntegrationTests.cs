using MarketOurs.Data;
using MarketOurs.Data.DataModels;
using MarketOurs.Data.DTOs;
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
/// Tests boundary values, extreme inputs, XSS, and null safety at the service layer.
/// MaxLength constraints (Title=128, Content=1024, Comment=512, Name=128) are from DataModels.
/// </summary>
[TestFixture]
[Category("Integration")]
public class InputBoundaryIntegrationTests : IntegrationTestBase
{
    private ServiceProvider _serviceProvider;
    private IPostService _postService;
    private ICommentService _commentService;
    private IUserService _userService;
    private IPostRepo _postRepo;
    private ICommentRepo _commentRepo;
    private IUserRepo _userRepo;
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

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<ICommentService, CommentService>();

        _serviceProvider = services.BuildServiceProvider();
        _postService = _serviceProvider.GetRequiredService<IPostService>();
        _commentService = _serviceProvider.GetRequiredService<ICommentService>();
        _userService = _serviceProvider.GetRequiredService<IUserService>();
        _postRepo = _serviceProvider.GetRequiredService<IPostRepo>();
        _commentRepo = _serviceProvider.GetRequiredService<ICommentRepo>();
        _userRepo = _serviceProvider.GetRequiredService<IUserRepo>();

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

    private async Task<UserModel> SeedUserAsync()
    {
        var user = new UserModel { Name = "Author", Email = "author@boundary.com", Password = "hash", IsActive = true };
        await _userRepo.CreateAsync(user);
        return user;
    }

    // ── Post Title Boundaries ────────────────────────────────────────────────

    [Test]
    public async Task CreatePost_TitleExactlyMaxLength_Persists()
    {
        var user = await SeedUserAsync();
        var title = new string('A', 128); // MaxLength = 128

        var result = await _postService.CreateAsync(new PostCreateDto
            { Title = title, Content = "Valid content", UserId = user.Id });

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Title.Length, Is.EqualTo(128));
    }

    [Test]
    public async Task CreatePost_SingleCharTitle_Persists()
    {
        var user = await SeedUserAsync();

        var result = await _postService.CreateAsync(new PostCreateDto
            { Title = "X", Content = "Content", UserId = user.Id });

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Title, Is.EqualTo("X"));
    }

    [Test]
    public async Task CreatePost_ExceedMaxTitleLength_ThrowsDbException()
    {
        var user = await SeedUserAsync();
        var overlong = new string('A', 129); // One over MaxLength

        Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(
            () => _postService.CreateAsync(new PostCreateDto
                { Title = overlong, Content = "Content", UserId = user.Id }));
    }

    // ── Post Content Boundaries ──────────────────────────────────────────────

    [Test]
    public async Task CreatePost_ContentAtMaxLength_Persists()
    {
        var user = await SeedUserAsync();
        var content = new string('C', 1024); // MaxLength = 1024

        var result = await _postService.CreateAsync(new PostCreateDto
            { Title = "Title", Content = content, UserId = user.Id });

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Content.Length, Is.EqualTo(1024));
    }

    [Test]
    public async Task CreatePost_ExceedMaxContentLength_ThrowsDbException()
    {
        var user = await SeedUserAsync();
        var overlong = new string('C', 1025);

        Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(
            () => _postService.CreateAsync(new PostCreateDto
                { Title = "Title", Content = overlong, UserId = user.Id }));
    }

    // ── Post Images ───────────────────────────────────────────────────────────

    [Test]
    public async Task CreatePost_EmptyImagesList_Persists()
    {
        var user = await SeedUserAsync();

        var result = await _postService.CreateAsync(new PostCreateDto
            { Title = "NoImages", Content = "Content", Images = [], UserId = user.Id });

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Images, Is.Empty);
    }

    [Test]
    public async Task CreatePost_ManyImages_Persists()
    {
        var user = await SeedUserAsync();
        var images = Enumerable.Range(0, 50).Select(i => $"https://img.example.com/{i}.jpg").ToList();

        var result = await _postService.CreateAsync(new PostCreateDto
            { Title = "ManyImages", Content = "Content", Images = images, UserId = user.Id });

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Images, Has.Count.EqualTo(50));
    }

    // ── XSS / Injection ──────────────────────────────────────────────────────

    [Test]
    public async Task CreatePost_XssPayloadInTitle_StoredAsLiteralString()
    {
        var user = await SeedUserAsync();
        var xssPayload = "<script>alert('xss')</script>";

        var result = await _postService.CreateAsync(new PostCreateDto
            { Title = xssPayload, Content = "Content", UserId = user.Id });

        // Service layer should store as-is; sanitization is the frontend's job
        Assert.That(result, Is.Not.Null);
        var dbPost = await _postRepo.GetByIdAsync(result!.Id);
        Assert.That(dbPost!.Title, Is.EqualTo(xssPayload),
            "XSS payload should be stored as a literal string, not executed");
    }

    [Test]
    public async Task CreatePost_SqlInjectionInContent_StoredAsLiteralString()
    {
        var user = await SeedUserAsync();
        var sqlPayload = "'; DROP TABLE posts; --";

        var result = await _postService.CreateAsync(new PostCreateDto
            { Title = "SQLTest", Content = sqlPayload, UserId = user.Id });

        Assert.That(result, Is.Not.Null);
        var dbPost = await _postRepo.GetByIdAsync(result!.Id);
        Assert.That(dbPost!.Content, Is.EqualTo(sqlPayload),
            "SQL injection payload should be stored safely via parameterized EF queries");
    }

    // ── Comment Depth ─────────────────────────────────────────────────────────

    [Test]
    public async Task CreateComment_DeepNesting_20Levels_AllPersist()
    {
        var user = await SeedUserAsync();
        var post = new PostModel { Title = "Deep Thread", Content = "Root", UserId = user.Id };
        await _postRepo.CreateAsync(post);

        string? parentId = null;
        for (int level = 0; level < 20; level++)
        {
            var comment = await _commentService.CreateAsync(new CommentCreateDto
            {
                Content = $"Level {level} reply",
                UserId = user.Id,
                PostId = post.Id,
                ParentCommentId = parentId
            });
            Assert.That(comment, Is.Not.Null, $"Comment at level {level} should be created");
            parentId = comment!.Id;
        }

        // Verify last comment is persisted
        var lastComment = await _commentRepo.GetByIdAsync(parentId!);
        Assert.That(lastComment, Is.Not.Null);
        Assert.That(lastComment!.Content, Is.EqualTo("Level 19 reply"));
    }

    // ── User with Non-ASCII / Emoji Name ─────────────────────────────────────

    [Test]
    public async Task CreateUser_UnicodeEmojiName_Persists()
    {
        var result = await _userService.CreateAsync(new UserCreateDto
            { Account = "emoji@test.com", Password = "Pass123", Name = "用户🐱‍👤" });

        Assert.That(result, Is.Not.Null);
        var dbUser = await _userRepo.GetByIdAsync(result.Id);
        Assert.That(dbUser!.Name, Is.EqualTo("用户🐱‍👤"));
    }

    // ── Non-Existent ID Queries ────────────────────────────────────────────────

    [Test]
    public async Task GetPostById_NonExistentId_ReturnsNull()
    {
        var result = await _postService.GetByIdAsync("00000000-0000-0000-0000-000000000000");
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task CreatePost_WithNonExistentUserId_ReturnsNull()
    {
        var result = await _postService.CreateAsync(new PostCreateDto
            { Title = "Orphan", Content = "Content", UserId = "non-existent-user" });

        Assert.That(result, Is.Null, "Post creation should fail for non-existent user");
    }

    private class TestDbContextFactory(DbContextOptions<MarketContext> options) : IDbContextFactory<MarketContext>
    {
        public MarketContext CreateDbContext() => new MarketContext(options);
    }
}
