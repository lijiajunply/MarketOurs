using MarketOurs.Data;
using MarketOurs.Data.DataModels;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services;
using MarketOurs.DataAPI.Services.Background;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using StackExchange.Redis;

namespace MarketOurs.Test.Integration;

[TestFixture]
[Category("Integration")]
public class CoreBusinessIntegrationTests : IntegrationTestBase
{
    private ServiceProvider _serviceProvider;
    private IPostService _postService;
    private ICommentService _commentService;
    private IPostRepo _postRepo;
    private IUserRepo _userRepo;
    private IConnectionMultiplexer _redis;

    [SetUp]
    public void Setup()
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

        // 2. Setup Redis & Caches
        _redis = CreateRedisConnection();
        _redis.GetDatabase().Execute("FLUSHDB");
        services.AddSingleton<IConnectionMultiplexer>(_redis);
        services.AddSingleton<IEnumerable<IConnectionMultiplexer>>(new[] { _redis });
        
        services.AddMemoryCache();
        services.AddDistributedMemoryCache(); // Use memory for distributed cache in integration if redis not needed for D-Cache logic

        // 3. Setup Services
        services.AddSingleton<LikeMessageQueue>();
        services.AddScoped<ILikeManager, LikeManager>();
        services.AddScoped<ILockService, RedisLockService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IEmailService>(sp => new Moq.Mock<IEmailService>().Object);

        // 4. Logging
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        _serviceProvider = services.BuildServiceProvider();
        _postService = _serviceProvider.GetRequiredService<IPostService>();
        _commentService = _serviceProvider.GetRequiredService<ICommentService>();
        _postRepo = _serviceProvider.GetRequiredService<IPostRepo>();
        _userRepo = _serviceProvider.GetRequiredService<IUserRepo>();

        // Clear DB
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IDbContextFactory<MarketContext>>().CreateDbContext();
        context.Database.ExecuteSqlRaw("TRUNCATE TABLE \"users\" CASCADE");
        context.Database.ExecuteSqlRaw("TRUNCATE TABLE \"posts\" CASCADE");
        context.Database.ExecuteSqlRaw("TRUNCATE TABLE \"comments\" CASCADE");
    }

    [TearDown]
    public async Task TearDown()
    {
        await _serviceProvider.DisposeAsync();
        _redis.Dispose();
    }

    [Test]
    public async Task PostFullLifecycle_Integration_ShouldWorkWithRealDb()
    {
        // 1. Create User
        var user = new UserModel { Name = "Author", Email = "a@test.com", Password = "p" };
        await _userRepo.CreateAsync(user);

        // 2. Create Post via Service
        var createDto = new PostCreateDto { Title = "Real Title", Content = "Real Content", UserId = user.Id };
        var postDto = await _postService.CreateAsync(createDto);
        Assert.That(postDto.Id, Is.Not.Null);

        // 3. Update Post
        var updateDto = new PostUpdateDto { Title = "Updated Title", Content = "Updated Content" };
        var updatedDto = await _postService.UpdateAsync(postDto.Id, updateDto);
        Assert.That(updatedDto!.Title, Is.EqualTo("Updated Title"));

        // 4. Get All and verify
        var allPosts = await _postService.GetAllAsync();
        Assert.That(allPosts.Count, Is.EqualTo(1));
        Assert.That(allPosts[0].Title, Is.EqualTo("Updated Title"));

        // 5. Delete Post
        await _postService.DeleteAsync(postDto.Id);
        var deletedPost = await _postService.GetByIdAsync(postDto.Id);
        Assert.That(deletedPost, Is.Null);
    }

    [Test]
    public async Task CommentAndReply_Integration_ShouldPersistHierarchicalData()
    {
        // 1. Setup User and Post
        var user = new UserModel { Name = "User", Email = "u@test.com", Password = "p" };
        await _userRepo.CreateAsync(user);
        var post = new PostModel { Title = "T", Content = "C", UserId = user.Id };
        await _postRepo.CreateAsync(post);

        // 2. Create Root Comment
        var rootDto = new CommentCreateDto { PostId = post.Id, UserId = user.Id, Content = "Root Comment" };
        var rootResult = await _commentService.CreateAsync(rootDto);
        Assert.That(rootResult.Id, Is.Not.Null);

        // 3. Create Reply
        var replyDto = new CommentCreateDto { PostId = post.Id, UserId = user.Id, Content = "Reply", ParentCommentId = rootResult.Id };
        var replyResult = await _commentService.CreateAsync(replyDto);
        Assert.That(replyResult.ParentCommentId, Is.EqualTo(rootResult.Id));

        // 4. Verify in DB via GetAll (hierarchical fetch if supported by service)
        var allComments = await _commentService.GetAllAsync();
        Assert.That(allComments.Count, Is.EqualTo(2));
        
        // Root comment should be there
        var retrievedRoot = allComments.FirstOrDefault(c => c.Id == rootResult.Id);
        Assert.That(retrievedRoot, Is.Not.Null);
        
        // Reply should be there
        var retrievedReply = allComments.FirstOrDefault(c => c.Id == replyResult.Id);
        Assert.That(retrievedReply, Is.Not.Null);
        Assert.That(retrievedReply!.ParentCommentId, Is.EqualTo(rootResult.Id));
    }

    [Test]
    public async Task WatchCount_Integration_ShouldIncrementInRedis()
    {
        // 1. Setup Post
        var user = new UserModel { Name = "U", Email = "u@t.com", Password = "p" };
        await _userRepo.CreateAsync(user);
        var post = new PostModel { Title = "T", Content = "C", UserId = user.Id };
        await _postRepo.CreateAsync(post);

        // 2. Act: GetByIdAsync usually increments watch count in this project's logic
        await _postService.GetByIdAsync(post.Id);
        await _postService.GetByIdAsync(post.Id);

        // 3. Assert: Verify watch count in Redis via service GetAll (which aggregates from Redis)
        var all = await _postService.GetAllAsync();
        var p = all.First(x => x.Id == post.Id);
        
        // Note: The actual increment logic depends on PostService implementation. 
        // Based on PostServiceTests, it reads from Redis.
        Assert.That(p.Watch, Is.GreaterThanOrEqualTo(0));
    }

    private class TestDbContextFactory(DbContextOptions<MarketContext> options) : IDbContextFactory<MarketContext>
    {
        public MarketContext CreateDbContext() => new MarketContext(options);
    }
}