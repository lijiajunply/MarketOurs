using MarketOurs.DataAPI.Configs;
using MarketOurs.Data;
using MarketOurs.Data.DataModels;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services;
using MarketOurs.DataAPI.Services.Background;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using StackExchange.Redis;

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
        
        // 4. Logging
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        _serviceProvider = services.BuildServiceProvider();
        _likeManager = _serviceProvider.GetRequiredService<ILikeManager>();
        _backgroundService = _serviceProvider.GetRequiredService<LikeSyncBackgroundService>();
        _postRepo = _serviceProvider.GetRequiredService<IPostRepo>();
        _userRepo = _serviceProvider.GetRequiredService<IUserRepo>();

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

    private class TestDbContextFactory(DbContextOptions<MarketContext> options) : IDbContextFactory<MarketContext>
    {
        public MarketContext CreateDbContext() => new MarketContext(options);
    }
}