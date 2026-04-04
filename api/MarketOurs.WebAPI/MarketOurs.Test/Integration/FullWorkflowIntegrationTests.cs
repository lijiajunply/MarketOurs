using MarketOurs.DataAPI.Configs;
using MarketOurs.Data;
using MarketOurs.Data.DTOs;
using MarketOurs.Data.DataModels;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services;
using MarketOurs.DataAPI.Services.Background;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;
using Moq;

namespace MarketOurs.Test.Integration;

[TestFixture]
[Category("Integration")]
public class FullWorkflowIntegrationTests : IntegrationTestBase
{
    private ServiceProvider _serviceProvider;
    private ILikeManager _likeManager;
    private LikeSyncBackgroundService _backgroundService;
    private IPostRepo _postRepo;
    private IUserRepo _userRepo;
    private IConnectionMultiplexer _redis;
    
    private IUserService _userService;
    private ILoginService _loginService;
    private IPostService _postService;
    
    private string _capturedEmailToken = string.Empty;

    [SetUp]
    public async Task Setup()
    {
        var services = new ServiceCollection();

        // 1. Setup DB
        var options = new DbContextOptionsBuilder<MarketContext>()
            .UseNpgsql(TestAssemblySetup.DbConnectionString)
            .Options;
        services.AddSingleton<IDbContextFactory<MarketContext>>(new TestDbContextFactory(options));
        services.AddScoped<IUserRepo, UserRepo>();
        services.AddScoped<IPostRepo, PostRepo>();
        services.AddScoped<ICommentRepo, CommentRepo>();

        // 2. Setup Redis & Lock
        _redis = CreateRedisConnection();
        _redis.GetDatabase().Execute("FLUSHDB");
        services.AddSingleton(_redis);
        services.AddSingleton<IEnumerable<IConnectionMultiplexer>>([_redis]);
        services.AddScoped<ILockService, RedisLockService>();

        // 3. Setup Like Management
        services.AddSingleton<LikeMessageQueue>();
        services.AddScoped<ILikeManager, LikeManager>();
        services.AddSingleton<LikeSyncBackgroundService>();
        
        // 4. Setup extra services for E2E flow
        services.AddMemoryCache();
        services.AddStackExchangeRedisCache(opt => 
        {
            opt.Configuration = TestAssemblySetup.RedisConnectionString;
        });

        var mockEmailService = new Mock<IEmailService>();
        mockEmailService.Setup(x => x.SendEmailWithTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
            .Callback<string, string, string, object, bool>((to, subject, template, model, isHtml) =>
            {
                var tokenProp = model?.GetType().GetProperty("token");
                if (tokenProp != null)
                {
                    _capturedEmailToken = tokenProp.GetValue(model)?.ToString() ?? string.Empty;
                }
            })
            .ReturnsAsync(true);
        services.AddSingleton(mockEmailService.Object);

        var mockJwtService = new Mock<IJwtService>();
        mockJwtService.Setup(x => x.GetAccessToken(It.IsAny<UserDto>(), It.IsAny<DeviceType>())).ReturnsAsync("dummy-access-token");
        mockJwtService.Setup(x => x.GetRefreshToken(It.IsAny<DeviceType>())).ReturnsAsync("dummy-refresh-token");
        services.AddSingleton(mockJwtService.Object);

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ILoginService, LoginService>();
        services.AddScoped<IPostService, PostService>();

        // 5. Logging
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        _serviceProvider = services.BuildServiceProvider();
        _likeManager = _serviceProvider.GetRequiredService<ILikeManager>();
        _backgroundService = _serviceProvider.GetRequiredService<LikeSyncBackgroundService>();
        _postRepo = _serviceProvider.GetRequiredService<IPostRepo>();
        _userRepo = _serviceProvider.GetRequiredService<IUserRepo>();
        _userService = _serviceProvider.GetRequiredService<IUserService>();
        _loginService = _serviceProvider.GetRequiredService<ILoginService>();
        _postService = _serviceProvider.GetRequiredService<IPostService>();

        // Clear DB
        using var scope = _serviceProvider.CreateScope();
        var context = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<MarketContext>>().CreateDbContextAsync();
        context.Database.ExecuteSqlRaw("TRUNCATE TABLE \"users\" CASCADE");
        context.Database.ExecuteSqlRaw("TRUNCATE TABLE \"posts\" CASCADE");

        // Start background service
        await _backgroundService.StartAsync(CancellationToken.None);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _backgroundService.StopAsync(CancellationToken.None);
        _backgroundService.Dispose();
        await _serviceProvider.DisposeAsync();
        _redis.Dispose();
    }

    [Test]
    public async Task FullLikeWorkflow_ShouldSyncRedisToDb()
    {
        // 1. Arrange: Create user and post in DB
        var user = new UserModel { Name = "User 1", Email = "u1@test.com", Password = "p" };
        await _userRepo.CreateAsync(user);

        var post = new PostModel { Title = "Post 1", Content = "Content", UserId = user.Id };
        await _postRepo.CreateAsync(post);

        // 2. Act: User likes post via LikeManager (goes to Redis + Queue)
        await _likeManager.SetPostLikeAsync(post.Id, user.Id);

        // 3. Verify Redis state immediately
        var db = _redis.GetDatabase();
        var redisLikes = await db.SetLengthAsync(CacheKeys.PostLikes(post.Id));
        Assert.That(redisLikes, Is.EqualTo(1));

        // 4. Wait for Background Service to process the queue
        // In a real integration test, we wait a bit or use a more robust polling mechanism
        int retries = 0;
        PostModel? updatedPost = null;
        while (retries < 10)
        {
            await Task.Delay(200);
            updatedPost = await _postRepo.GetByIdAsync(post.Id);
            if (updatedPost?.Likes > 0) break;
            retries++;
        }

        // 5. Assert: DB state should be updated
        Assert.That(updatedPost, Is.Not.Null);
        Assert.That(updatedPost!.Likes, Is.EqualTo(1), "Database should be updated by background service");
        
        var likeUsers = await _postRepo.GetLikeUsersAsync(post.Id);
        Assert.That(likeUsers!.Any(u => u.Id == user.Id), Is.True);

        // 6. Act 2: User un-likes post
        await _likeManager.SetPostLikeAsync(post.Id, user.Id);

        // 7. Wait and Verify
        retries = 0;
        while (retries < 10)
        {
            await Task.Delay(200);
            updatedPost = await _postRepo.GetByIdAsync(post.Id);
            if (updatedPost?.Likes == 0) break;
            retries++;
        }
        Assert.That(updatedPost!.Likes, Is.EqualTo(0));
    }

    [Test]
    public async Task FullUserJourney_E2E_Workflow_ShouldSucceed()
    {
        // 1. User Registration
        var account = "e2e_user@example.com";
        var password = "SecurePassword123";
        var name = "E2E Test User";

        var createDto = new UserCreateDto
        {
            Account = account,
            Password = password,
            Name = name
        };
        var createdUser = await _userService.CreateAsync(createDto);
        
        Assert.That(createdUser, Is.Not.Null);
        Assert.That(createdUser.Email, Is.EqualTo(account));

        var dbUser = await _userRepo.GetByIdAsync(createdUser.Id);
        Assert.That(dbUser, Is.Not.Null);
        Assert.That(dbUser!.IsEmailVerified, Is.False, "New user should not have email verified yet.");

        // 2. Email Verification
        var emailSent = await _userService.SendVerificationEmailAsync(createdUser.Id);
        Assert.That(emailSent, Is.True, "Verification email should be sent.");
        Assert.That(_capturedEmailToken, Is.Not.Empty, "Token should have been captured from mock email service.");

        var verifyResult = await _userService.VerifyEmailAsync(_capturedEmailToken);
        Assert.That(verifyResult, Is.True, "Email verification should succeed.");

        dbUser = await _userRepo.GetByIdAsync(createdUser.Id);
        Assert.That(dbUser!.IsEmailVerified, Is.True, "Database should reflect email verified status.");

        // 3. Login
        var tokenDto = await _loginService.Login(account, password, DeviceType.Web.ToString());
        Assert.That(tokenDto, Is.Not.Null, "Login should succeed after verification.");
        Assert.That(tokenDto.AccessToken, Is.EqualTo("dummy-access-token"));
        Assert.That(tokenDto.RefreshToken, Is.EqualTo("dummy-refresh-token"));

        // 4. Post Creation
        var postCreateDto = new PostCreateDto
        {
            Title = "My First Post",
            Content = "This is a great journey!",
            UserId = createdUser.Id,
            Images = ["img1.jpg", "img2.jpg"]
        };
        
        var createdPost = await _postService.CreateAsync(postCreateDto);
        Assert.That(createdPost, Is.Not.Null);
        Assert.That(createdPost!.Title, Is.EqualTo(postCreateDto.Title));
        
        var dbPost = await _postRepo.GetByIdAsync(createdPost.Id);
        Assert.That(dbPost, Is.Not.Null);
        Assert.That(dbPost!.UserId, Is.EqualTo(createdUser.Id));
        Assert.That(dbPost.Likes, Is.EqualTo(0));

        // 5. Like the Post
        await _likeManager.SetPostLikeAsync(createdPost.Id, createdUser.Id);

        // Verify Redis updated
        var db = _redis.GetDatabase();
        var redisLikes = await db.SetLengthAsync(CacheKeys.PostLikes(createdPost.Id));
        Assert.That(redisLikes, Is.EqualTo(1), "Redis should show 1 like.");

        // Wait for LikeSyncBackgroundService to persist
        int retries = 0;
        PostModel? updatedPost = null;
        while (retries < 15)
        {
            await Task.Delay(200);
            updatedPost = await _postRepo.GetByIdAsync(createdPost.Id);
            if (updatedPost?.Likes > 0) break;
            retries++;
        }

        Assert.That(updatedPost, Is.Not.Null);
        Assert.That(updatedPost!.Likes, Is.EqualTo(1), "Post likes should be synced to DB.");
    }

    private class TestDbContextFactory(DbContextOptions<MarketContext> options) : IDbContextFactory<MarketContext>
    {
        public MarketContext CreateDbContext() => new MarketContext(options);
    }
}
