using MarketOurs.Data;
using MarketOurs.Data.DataModels;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services;
using MarketOurs.DataAPI.Services.Background;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;

namespace MarketOurs.Test.Integration;

[TestFixture]
[Category("Integration")]
public class ConcurrencyStressIntegrationTests : IntegrationTestBase
{
    private ServiceProvider _serviceProvider;
    private ILikeManager _likeManager;
    private LikeSyncBackgroundService _backgroundService;
    private IPostRepo _postRepo;
    private IUserRepo _userRepo;
    private IConnectionMultiplexer _redis;
    private IDbContextFactory<MarketContext> _dbFactory;

    [SetUp]
    public async Task Setup()
    {
        var services = new ServiceCollection();

        // DB
        var options = new DbContextOptionsBuilder<MarketContext>()
            .UseNpgsql(TestAssemblySetup.DbConnectionString)
            // Need a larger connection pool or enable retries for high concurrency tests
            .EnableDetailedErrors()
            .Options;
            
        _dbFactory = new TestDbContextFactory(options);
        services.AddSingleton<IDbContextFactory<MarketContext>>(_dbFactory);
        services.AddScoped<IUserRepo, UserRepo>();
        services.AddScoped<IPostRepo, PostRepo>();
        services.AddScoped<ICommentRepo, CommentRepo>();

        // Redis & Lock
        _redis = CreateRedisConnection();
        _redis.GetDatabase().Execute("FLUSHDB");
        services.AddSingleton<IConnectionMultiplexer>(_redis);
        services.AddSingleton<IEnumerable<IConnectionMultiplexer>>(new[] { _redis });
        services.AddScoped<ILockService, RedisLockService>();

        // Like Management
        services.AddSingleton<LikeMessageQueue>();
        services.AddScoped<ILikeManager, LikeManager>();
        services.AddSingleton<LikeSyncBackgroundService>();
        
        // Logging
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        _serviceProvider = services.BuildServiceProvider();
        _likeManager = _serviceProvider.GetRequiredService<ILikeManager>();
        _backgroundService = _serviceProvider.GetRequiredService<LikeSyncBackgroundService>();
        _postRepo = _serviceProvider.GetRequiredService<IPostRepo>();
        _userRepo = _serviceProvider.GetRequiredService<IUserRepo>();

        // Clear DB
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IDbContextFactory<MarketContext>>().CreateDbContext();
        context.Database.ExecuteSqlRaw("TRUNCATE TABLE \"users\" CASCADE");
        context.Database.ExecuteSqlRaw("TRUNCATE TABLE \"posts\" CASCADE");
        
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
    [CancelAfter(30000)] // High concurrency test should finish reasonably fast
    public async Task HighConcurrency_Likes_ShouldProcessAllCorrectly()
    {
        // 1. Arrange: Create 100 users and 1 post
        var users = new List<UserModel>();
        for (int i = 0; i < 100; i++)
        {
            var u = new UserModel { Name = $"U{i}", Email = $"u{i}@test.com", Password = "p" };
            await _userRepo.CreateAsync(u);
            users.Add(u);
        }

        var post = new PostModel { Title = "Hot Post", Content = "C", UserId = users[0].Id };
        await _postRepo.CreateAsync(post);

        // 2. Act: All 100 users like the post concurrently
        // We use Parallel.ForEachAsync to smash the API
        await Parallel.ForEachAsync(users, new ParallelOptions { MaxDegreeOfParallelism = 50 }, async (u, ct) =>
        {
            using var scope = _serviceProvider.CreateScope();
            var scopedLikeManager = scope.ServiceProvider.GetRequiredService<ILikeManager>();
            await scopedLikeManager.SetPostLikeAsync(post.Id, u.Id);
        });

        // 3. Wait for background queue to flush
        int waitTime = 0;
        int maxWait = 10000; // 10 seconds max wait for DB sync
        PostModel? finalPost = null;

        while (waitTime < maxWait)
        {
            using var scope = _serviceProvider.CreateScope();
            var scopedPostRepo = scope.ServiceProvider.GetRequiredService<IPostRepo>();
            finalPost = await scopedPostRepo.GetByIdAsync(post.Id);

            if (finalPost?.Likes == 100)
            {
                break;
            }
            await Task.Delay(500);
            waitTime += 500;
        }

        // 4. Assert
        Assert.That(finalPost, Is.Not.Null);
        // The DB Likes should precisely equal the number of concurrent operations (no lost updates)
        Assert.That(finalPost!.Likes, Is.EqualTo(100), "Lost update detected under high concurrency.");
        
        var redisLikes = await _likeManager.GetPostLikesAsync(post.Id, 0);
        Assert.That(redisLikes, Is.EqualTo(100), "Redis cache count mismatch.");
    }
    
    [Test]
    public async Task Concurrency_ToggleLikeDislike_ShouldLeaveConsistentState()
    {
        // Arrange
        var user = new UserModel { Name = "Toggler", Email = "tog@test.com", Password = "p" };
        await _userRepo.CreateAsync(user);

        var post = new PostModel { Title = "T", Content = "C", UserId = user.Id };
        await _postRepo.CreateAsync(post);

        // Act: Concurrently Like and Dislike by the SAME user multiple times
        var actions = Enumerable.Range(0, 20).Select(i => 
            i % 2 == 0 ? _likeManager.SetPostLikeAsync(post.Id, user.Id) : _likeManager.SetPostDislikeAsync(post.Id, user.Id)
        );

        await Task.WhenAll(actions);

        // Assert
        // In Redis, a user can only be in either the like set or dislike set, not both.
        // Also the total length of both sets combined should not exceed 1 (since it's only 1 user).
        var db = _redis.GetDatabase();
        var likeCount = await db.SetLengthAsync(DataAPI.Configs.CacheKeys.PostLikes(post.Id));
        var dislikeCount = await db.SetLengthAsync(DataAPI.Configs.CacheKeys.PostDislikes(post.Id));

        Assert.That(likeCount + dislikeCount, Is.LessThanOrEqualTo(1), "A user should not generate multiple likes/dislikes under concurrency.");
    }

    private class TestDbContextFactory(DbContextOptions<MarketContext> options) : IDbContextFactory<MarketContext>
    {
        public MarketContext CreateDbContext() => new MarketContext(options);
    }
}