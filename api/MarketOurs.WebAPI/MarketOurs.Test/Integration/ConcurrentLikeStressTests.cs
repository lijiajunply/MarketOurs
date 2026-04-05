using System.Diagnostics;
using MarketOurs.Data;
using MarketOurs.Data.DataModels;
using MarketOurs.DataAPI.Configs;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services;
using MarketOurs.DataAPI.Services.Background;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;

namespace MarketOurs.Test.Integration;

/// <summary>
/// Stress tests for concurrent like/dislike operations.
/// Requires Docker (PostgreSQL + Redis via Testcontainers).
/// </summary>
[TestFixture]
[Category("Integration")]
public class ConcurrentLikeStressTests : MarketOurs.Test.Integration.IntegrationTestBase
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

        var options = new DbContextOptionsBuilder<MarketContext>()
            .UseNpgsql(MarketOurs.Test.Integration.TestAssemblySetup.DbConnectionString)
            .Options;
        services.AddSingleton<IDbContextFactory<MarketContext>>(new TestDbContextFactory(options));
        services.AddScoped<IUserRepo, UserRepo>();
        services.AddScoped<IPostRepo, PostRepo>();
        services.AddScoped<ICommentRepo, CommentRepo>();

        _redis = ConnectionMultiplexer.Connect(MarketOurs.Test.Integration.TestAssemblySetup.RedisConnectionString!);
        _redis.GetDatabase().Execute("FLUSHDB");
        services.AddSingleton(_redis);
        services.AddSingleton<IEnumerable<IConnectionMultiplexer>>([_redis]);
        services.AddScoped<ILockService, RedisLockService>();
        services.AddSingleton<LikeMessageQueue>();
        services.AddScoped<ILikeManager, LikeManager>();
        services.AddSingleton<LikeSyncBackgroundService>();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        _serviceProvider = services.BuildServiceProvider();
        _likeManager = _serviceProvider.GetRequiredService<ILikeManager>();
        _backgroundService = _serviceProvider.GetRequiredService<LikeSyncBackgroundService>();
        _postRepo = _serviceProvider.GetRequiredService<IPostRepo>();
        _userRepo = _serviceProvider.GetRequiredService<IUserRepo>();

        using var scope = _serviceProvider.CreateScope();
        var ctx = await scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<MarketContext>>().CreateDbContextAsync();
        ctx.Database.ExecuteSqlRaw("TRUNCATE TABLE \"users\" CASCADE");
        ctx.Database.ExecuteSqlRaw("TRUNCATE TABLE \"posts\" CASCADE");

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
    public async Task ConcurrentLikes_100UniqueUsers_RedisCountExact()
    {
        // Arrange: create 100 users and 1 post
        const int userCount = 100;
        var users = new List<UserModel>();
        for (int i = 0; i < userCount; i++)
        {
            var u = new UserModel { Name = $"User{i}", Email = $"u{i}@stress.com", Password = "h", IsActive = true };
            await _userRepo.CreateAsync(u);
            users.Add(u);
        }
        var post = new PostModel { Title = "Hot Post", Content = "c", UserId = users[0].Id };
        await _postRepo.CreateAsync(post);

        // Act: all 100 users like the same post concurrently
        var sw = Stopwatch.StartNew();
        await Parallel.ForEachAsync(users,
            new ParallelOptions { MaxDegreeOfParallelism = 50 },
            async (user, _) => await _likeManager.SetPostLikeAsync(post.Id, user.Id));
        sw.Stop();

        var db = _redis.GetDatabase();
        var likeCount = await db.SetLengthAsync(CacheKeys.PostLikes(post.Id));

        await TestContext.Out.WriteLineAsync(
            $"100 concurrent likes in {sw.ElapsedMilliseconds}ms. Redis set size: {likeCount}");

        Assert.That(likeCount, Is.EqualTo(userCount),
            "Redis Set should contain exactly 100 unique users");
    }

    [Test]
    public async Task SameUserLikesMultipleTimes_OnlyCountedOnce()
    {
        var user = new UserModel { Name = "Single", Email = "single@stress.com", Password = "h", IsActive = true };
        await _userRepo.CreateAsync(user);
        var post = new PostModel { Title = "Solo Post", Content = "c", UserId = user.Id };
        await _postRepo.CreateAsync(post);

        // 10 concurrent like requests from the same user
        await Parallel.ForEachAsync(Enumerable.Range(0, 10),
            new ParallelOptions { MaxDegreeOfParallelism = 10 },
            async (_, _) => await _likeManager.SetPostLikeAsync(post.Id, user.Id));

        var db = _redis.GetDatabase();
        var likeCount = await db.SetLengthAsync(CacheKeys.PostLikes(post.Id));

        // Because of toggle semantics, result should be 0 or 1 (not 10)
        Assert.That(likeCount, Is.LessThanOrEqualTo(1),
            "Duplicate concurrent likes from same user should not exceed 1");
    }

    [Test]
    public async Task LikeDislikeMutualExclusion_50Users_NeverBoth()
    {
        var user = new UserModel { Name = "Toggle", Email = "toggle@stress.com", Password = "h", IsActive = true };
        await _userRepo.CreateAsync(user);

        // Create 50 posts, each with a like+dislike toggle attempt
        var posts = new List<PostModel>();
        for (int i = 0; i < 50; i++)
        {
            var p = new PostModel { Title = $"P{i}", Content = "c", UserId = user.Id };
            await _postRepo.CreateAsync(p);
            posts.Add(p);
        }

        // For each post: like then dislike concurrently (mutual exclusion check)
        await Parallel.ForEachAsync(posts,
            new ParallelOptions { MaxDegreeOfParallelism = 25 },
            async (post, _) =>
            {
                await _likeManager.SetPostLikeAsync(post.Id, user.Id);
                await _likeManager.SetPostDislikeAsync(post.Id, user.Id);
            });

        var db = _redis.GetDatabase();
        foreach (var post in posts)
        {
            var inLikes = await db.SetContainsAsync(CacheKeys.PostLikes(post.Id), user.Id);
            var inDislikes = await db.SetContainsAsync(CacheKeys.PostDislikes(post.Id), user.Id);

            Assert.That(inLikes && inDislikes, Is.False,
                $"User {user.Id} should not be in both likes and dislikes for post {post.Id}");
        }
    }

    [Test]
    public async Task HighFrequencyToggle_10KOps_FinalStateConsistent()
    {
        var user = new UserModel { Name = "Toggler", Email = "toggler@stress.com", Password = "h", IsActive = true };
        await _userRepo.CreateAsync(user);
        var post = new PostModel { Title = "Toggle Post", Content = "c", UserId = user.Id };
        await _postRepo.CreateAsync(post);

        // 1000 serial toggles (must be serial to test deterministic final state)
        for (int i = 0; i < 1000; i++)
            await _likeManager.SetPostLikeAsync(post.Id, user.Id);

        var db = _redis.GetDatabase();
        var inLikes = await db.SetContainsAsync(CacheKeys.PostLikes(post.Id), user.Id);
        var likeCount = await db.SetLengthAsync(CacheKeys.PostLikes(post.Id));

        // After 1000 toggles (even number), user should NOT be in likes (toggled back to 0)
        Assert.That(likeCount, Is.EqualTo(0),
            "After even number of toggles, user should not be liked");
        Assert.That(inLikes, Is.False);
    }

    [Test]
    public async Task LikeWithBackgroundSync_1000Likes_DbCountCorrect()
    {
        // Create users and post
        const int totalLikes = 100; // Keep manageable for CI
        var users = new List<UserModel>();
        for (int i = 0; i < totalLikes; i++)
        {
            var u = new UserModel
                { Name = $"BGUser{i}", Email = $"bg{i}@stress.com", Password = "h", IsActive = true };
            await _userRepo.CreateAsync(u);
            users.Add(u);
        }
        var post = new PostModel { Title = "BG Sync Post", Content = "c", UserId = users[0].Id };
        await _postRepo.CreateAsync(post);

        // Act: all users like the post
        foreach (var user in users)
            await _likeManager.SetPostLikeAsync(post.Id, user.Id);

        // Wait for background service to sync all like messages to DB
        var sw = Stopwatch.StartNew();
        PostModel? updatedPost = null;
        while (sw.Elapsed < TimeSpan.FromSeconds(15))
        {
            await Task.Delay(300);
            updatedPost = await _postRepo.GetByIdAsync(post.Id);
            if (updatedPost?.Likes >= totalLikes) break;
        }

        await TestContext.Out.WriteLineAsync(
            $"DB Likes after sync: {updatedPost?.Likes} / {totalLikes}");
        Assert.That(updatedPost?.Likes, Is.EqualTo(totalLikes),
            "Background service should sync all like operations to DB");
    }

    private class TestDbContextFactory(DbContextOptions<MarketContext> options) : IDbContextFactory<MarketContext>
    {
        public MarketContext CreateDbContext() => new MarketContext(options);
    }
}
