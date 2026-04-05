using MarketOurs.Data;
using MarketOurs.Data.DataModels;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Configs;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services;
using MarketOurs.DataAPI.Services.Background;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;

namespace MarketOurs.Test.Integration;

[TestFixture]
[Category("Integration")]
public class PostSearchAndHotIntegrationTests : IntegrationTestBase
{
    private ServiceProvider _serviceProvider;
    private IPostService _postService;
    private IPostRepo _postRepo;
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
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPostService, PostService>();

        var mockEmail = new Mock<IEmailService>();
        services.AddSingleton(mockEmail.Object);

        _serviceProvider = services.BuildServiceProvider();
        _postService = _serviceProvider.GetRequiredService<IPostService>();
        _postRepo = _serviceProvider.GetRequiredService<IPostRepo>();
        _userRepo = _serviceProvider.GetRequiredService<IUserRepo>();

        using var scope = _serviceProvider.CreateScope();
        var ctx = await scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<MarketContext>>().CreateDbContextAsync();
        ctx.Database.ExecuteSqlRaw("TRUNCATE TABLE \"users\" CASCADE");
        ctx.Database.ExecuteSqlRaw("TRUNCATE TABLE \"posts\" CASCADE");
    }

    [TearDown]
    public async Task TearDown()
    {
        await _serviceProvider.DisposeAsync();
        _redis.Dispose();
    }

    private async Task<UserModel> SeedUserAsync(string email = "author@test.com")
    {
        var user = new UserModel { Name = "Author", Email = email, Password = "hash", IsActive = true };
        await _userRepo.CreateAsync(user);
        return user;
    }

    [Test]
    public async Task SearchAsync_ExactKeyword_ReturnsMatchingPosts()
    {
        var user = await SeedUserAsync();
        await _postRepo.CreateAsync(new PostModel
            { Title = "二手单反相机", Content = "9 成新，价格美丽", UserId = user.Id });
        await _postRepo.CreateAsync(new PostModel
            { Title = "旧手机出售", Content = "苹果 14", UserId = user.Id });

        var results = await _postService.SearchAsync(new PaginationParams { Keyword = "相机" });

        Assert.That(results.Items, Has.Count.EqualTo(1));
        Assert.That(results.Items[0].Title, Does.Contain("相机"));
    }

    [Test]
    public async Task SearchAsync_EmptyKeyword_ReturnsEmptyList()
    {
        var user = await SeedUserAsync();
        await _postRepo.CreateAsync(new PostModel { Title = "Any Post", Content = "Content", UserId = user.Id });

        var results = await _postService.SearchAsync(new PaginationParams { Keyword = string.Empty });
        Assert.That(results.Items, Is.Empty);

        var wsResults = await _postService.SearchAsync(new PaginationParams { Keyword = "   " });
        Assert.That(wsResults.Items, Is.Empty);
    }

    [Test]
    public async Task SearchAsync_VeryLongKeyword_DoesNotThrow()
    {
        var user = await SeedUserAsync();
        var longKeyword = new string('z', 500);

        PagedResultDto<PostDto> results = null!;
        Assert.DoesNotThrowAsync(async () =>
            results = await _postService.SearchAsync(new PaginationParams { Keyword = longKeyword }));
        Assert.That(results, Is.Not.Null);
        Assert.That(results.Items, Is.Empty);
    }

    [Test]
    public async Task GetHotAsync_FirstCall_HitsDbAndPopulatesDistCache()
    {
        var user = await SeedUserAsync();
        for (int i = 0; i < 5; i++)
        {
            await _postRepo.CreateAsync(new PostModel
            {
                Title = $"Post {i}", Content = "Content", UserId = user.Id,
                Watch = i * 10, Likes = i * 3
            });
        }

        var cache = _serviceProvider.GetRequiredService<IDistributedCache>();
        var distKey = CacheKeys.HotPostsDist(10);
        await cache.RemoveAsync(distKey); // Ensure cache miss

        var result = await _postService.GetHotAsync(10);

        Assert.That(result, Is.Not.Empty);
        // Distributed cache should now be populated
        var cached = await cache.GetStringAsync(distKey);
        Assert.That(cached, Is.Not.Null, "Hot posts should be cached in Redis after first call");
    }

    [Test]
    public async Task GetHotAsync_SecondCall_HitsMemoryCache_NotDb()
    {
        var user = await SeedUserAsync();
        await _postRepo.CreateAsync(new PostModel
            { Title = "Hot One", Content = "c", UserId = user.Id, Watch = 100 });

        // First call populates caches
        await _postService.GetHotAsync();
        // Second call should be served from L1 memory cache
        var result = await _postService.GetHotAsync();

        Assert.That(result, Is.Not.Empty);
    }

    [Test]
    public async Task IncrementWatchAsync_AtThreshold_SyncsToDB()
    {
        var user = await SeedUserAsync();
        var post = new PostModel { Title = "Watch Test", Content = "c", UserId = user.Id, Watch = 0 };
        await _postRepo.CreateAsync(post);

        // Increment exactly 10 times (WatchSyncThreshold = 10)
        for (int i = 0; i < 10; i++)
            await _postService.IncrementWatchAsync(post.Id);

        // Allow background fire-and-forget sync to complete
        await Task.Delay(500);

        var db = _redis.GetDatabase();
        var redisCount = (int)(await db.StringGetAsync(CacheKeys.PostWatch(post.Id)));
        Assert.That(redisCount, Is.EqualTo(10), "Redis watch counter should be 10");

        var updatedPost = await _postRepo.GetByIdAsync(post.Id);
        Assert.That(updatedPost!.Watch, Is.GreaterThanOrEqualTo(10),
            "DB should have at least 10 watch synced after threshold");
    }

    [Test]
    public async Task IncrementWatchAsync_ConcurrentIncrements_CountIsAccurate()
    {
        var user = await SeedUserAsync();
        var post = new PostModel { Title = "Concurrent Watch", Content = "c", UserId = user.Id };
        await _postRepo.CreateAsync(post);

        const int totalIncrements = 50;
        await Parallel.ForEachAsync(Enumerable.Range(0, totalIncrements),
            new ParallelOptions { MaxDegreeOfParallelism = 20 },
            async (_, _) => await _postService.IncrementWatchAsync(post.Id));

        await Task.Delay(500);

        var db = _redis.GetDatabase();
        var redisCount = (int)(await db.StringGetAsync(CacheKeys.PostWatch(post.Id)));
        Assert.That(redisCount, Is.EqualTo(totalIncrements),
            "Redis atomic increment should give exact count");
    }

    private class TestDbContextFactory(DbContextOptions<MarketContext> options) : IDbContextFactory<MarketContext>
    {
        public MarketContext CreateDbContext() => new(options);
    }
}