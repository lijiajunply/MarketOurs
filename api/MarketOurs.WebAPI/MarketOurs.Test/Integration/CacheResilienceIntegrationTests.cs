using MarketOurs.Data;
using MarketOurs.Data.DataModels;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Configs;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services;
using MarketOurs.DataAPI.Services.Background;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;

namespace MarketOurs.Test.Integration;

[TestFixture]
[Category("Integration")]
public class CacheResilienceIntegrationTests : IntegrationTestBase
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

        var mockEmail = new Mock<IEmailService>();
        services.AddSingleton(mockEmail.Object);
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPostService, PostService>();

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

    private async Task<(UserModel user, PostModel post)> SeedAsync()
    {
        var user = new UserModel { Name = "CacheUser", Email = "cache@test.com", Password = "hash", IsActive = true };
        await _userRepo.CreateAsync(user);
        var post = new PostModel { Title = "Cache Post", Content = "Original Content", UserId = user.Id };
        await _postRepo.CreateAsync(post);
        return (user, post);
    }

    [Test]
    public async Task GetByIdAsync_CacheMiss_FallsBackToDbAndRebuildsCaches()
    {
        var (_, post) = await SeedAsync();

        // Ensure both caches empty
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(CacheKeys.PostDist(post.Id));

        var result = await _postService.GetByIdAsync(post.Id);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Title, Is.EqualTo("Cache Post"));

        // Distributed cache should now be populated
        var distCached = await db.StringGetAsync(CacheKeys.PostDist(post.Id));
        Assert.That(distCached.HasValue, Is.True, "Redis cache should be populated after DB fallback");
    }

    [Test]
    public async Task UpdatePost_InvalidatesCache_NextReadGetsNewValue()
    {
        var (_, post) = await SeedAsync();

        // Prime the cache
        await _postService.GetByIdAsync(post.Id);

        // Update
        await _postService.UpdateAsync(post.Id, new PostUpdateDto
            { Title = "Updated Title", Content = "New Content" });

        // Next read should reflect update (cache was invalidated)
        var result = await _postService.GetByIdAsync(post.Id);
        Assert.That(result!.Title, Is.EqualTo("Updated Title"),
            "After update, cache should be invalidated and new value returned");
    }

    [Test]
    public async Task DeletePost_InvalidatesCache_GetByIdReturnsNull()
    {
        var (_, post) = await SeedAsync();

        // Prime the cache
        await _postService.GetByIdAsync(post.Id);

        // Delete
        await _postService.DeleteAsync(post.Id);

        // Cache should be invalidated and DB should return null
        var result = await _postService.GetByIdAsync(post.Id);
        Assert.That(result, Is.Null, "Deleted post should not be retrievable from cache or DB");
    }

    [Test]
    public async Task GetByIdAsync_ForceExpiredDistCache_RebuildFromDb()
    {
        var (_, post) = await SeedAsync();

        // Prime distributed cache
        await _postService.GetByIdAsync(post.Id);

        // Manually expire the Redis key
        var db = _redis.GetDatabase();
        await db.KeyExpireAsync(CacheKeys.PostDist(post.Id), TimeSpan.FromMilliseconds(1));
        await Task.Delay(10); // Let it expire

        // Should still return data (fallback to DB)
        var result = await _postService.GetByIdAsync(post.Id);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Title, Is.EqualTo("Cache Post"));
    }

    [Test]
    public async Task GetByIdAsync_ConcurrentRequestsSameId_AllReturnCorrectData()
    {
        var (_, post) = await SeedAsync();

        // Flush all caches to force cache miss
        _redis.GetDatabase().Execute("FLUSHDB");
        var memCache = _serviceProvider.GetRequiredService<IMemoryCache>();
        // Reset memory cache by getting a fresh service scope
        await _serviceProvider.DisposeAsync();

        // Rebuild service with fresh caches
        var services = new ServiceCollection();
        var options = new DbContextOptionsBuilder<MarketContext>()
            .UseNpgsql(TestAssemblySetup.DbConnectionString).Options;
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
        services.AddSingleton(new Mock<IEmailService>().Object);
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPostService, PostService>();
        _serviceProvider = services.BuildServiceProvider();
        _postService = _serviceProvider.GetRequiredService<IPostService>();

        // Fire 30 concurrent requests for the same post ID
        var tasks = Enumerable.Range(0, 30).Select(_ => _postService.GetByIdAsync(post.Id));
        var results = await Task.WhenAll(tasks);

        Assert.That(results, Has.All.Not.Null,
            "All concurrent requests should return the post without null");
        Assert.That(results.Select(r => r!.Id).Distinct().Count(), Is.EqualTo(1),
            "All results should be for the same post");
    }

    [Test]
    public async Task GetHotAsync_CacheExpiry_RebuildFromDb()
    {
        var user = new UserModel { Name = "U", Email = "hot@test.com", Password = "h", IsActive = true };
        await _userRepo.CreateAsync(user);
        await _postRepo.CreateAsync(new PostModel
            { Title = "Hot Post", Content = "c", UserId = user.Id, Watch = 999 });

        // Prime cache
        await _postService.GetHotAsync();

        // Expire distributed cache key
        var db = _redis.GetDatabase();
        await db.KeyExpireAsync(CacheKeys.HotPostsDist(10), TimeSpan.FromMilliseconds(1));
        await Task.Delay(10);

        // Should rebuild from DB without error
        var result = await _postService.GetHotAsync();
        Assert.That(result, Is.Not.Empty);
    }

    private class TestDbContextFactory(DbContextOptions<MarketContext> options) : IDbContextFactory<MarketContext>
    {
        public MarketContext CreateDbContext() => new MarketContext(options);
    }
}
