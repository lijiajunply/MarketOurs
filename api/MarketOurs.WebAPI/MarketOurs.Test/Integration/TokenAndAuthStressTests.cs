using System.Diagnostics;
using MarketOurs.Data;
using MarketOurs.Data.DataModels;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Configs;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;

namespace MarketOurs.Test.Integration;

/// <summary>
/// Stress tests for token management and authentication operations under high concurrency.
/// </summary>
[TestFixture]
[Category("Integration")]
public class TokenAndAuthStressTests : MarketOurs.Test.Integration.IntegrationTestBase
{
    private ServiceProvider _serviceProvider;
    private ILoginService _loginService;
    private IUserService _userService;
    private IUserRepo _userRepo;
    private IConnectionMultiplexer _redis;

    [SetUp]
    public async Task Setup()
    {
        if (!MarketOurs.Test.Integration.TestAssemblySetup.IsDockerAvailable)
        {
            return;
        }

        var services = new ServiceCollection();

        var options = new DbContextOptionsBuilder<MarketContext>()
            .UseNpgsql(MarketOurs.Test.Integration.TestAssemblySetup.DbConnectionString!)
            .Options;
        services.AddSingleton<IDbContextFactory<MarketContext>>(new TestDbContextFactory(options));
        services.AddScoped<IUserRepo, UserRepo>();
        services.AddScoped<IPostRepo, PostRepo>();
        services.AddScoped<ICommentRepo, CommentRepo>();

        _redis = ConnectionMultiplexer.Connect(MarketOurs.Test.Integration.TestAssemblySetup.RedisConnectionString!);
        _redis.GetDatabase().Execute("FLUSHDB");
        services.AddSingleton(_redis);
        services.AddSingleton<IEnumerable<IConnectionMultiplexer>>([_redis]);

        var mockEmail = new Mock<IEmailService>();
        services.AddSingleton(mockEmail.Object);

        // Use real-ish JWT mock that returns unique tokens per user
        var mockJwt = new Mock<IJwtService>();
        mockJwt.Setup(x => x.GetAccessToken(It.IsAny<UserDto>(), It.IsAny<DeviceType>()))
            .ReturnsAsync((UserDto u, DeviceType _) => $"token-{u.Id}");
        mockJwt.Setup(x => x.GetRefreshToken(It.IsAny<DeviceType>()))
            .ReturnsAsync(() => $"refresh-{Guid.NewGuid():N}");
        services.AddSingleton(mockJwt.Object);

        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ILoginService, LoginService>();

        _serviceProvider = services.BuildServiceProvider();
        _loginService = _serviceProvider.GetRequiredService<ILoginService>();
        _userService = _serviceProvider.GetRequiredService<IUserService>();
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
        if (_serviceProvider != null) await _serviceProvider.DisposeAsync();
        _redis?.Dispose();
    }

    [Test]
    public async Task ConcurrentTokenValidation_1000ValidTokens_AllPass()
    {
        // Create 100 users and log them in
        const int userCount = 100;
        var tokenData = new List<(string userId, string token)>();

        for (int i = 0; i < userCount; i++)
        {
            var user = new UserModel
                { Name = $"U{i}", Email = $"val{i}@stress.com", Password = "hash".StringToHash(), IsActive = true };
            await _userRepo.CreateAsync(user);
            var tokenDto = await _loginService.Login(user.Email, "hash", "Web");
            tokenData.Add((user.Id, tokenDto.AccessToken));
        }

        // 1000 concurrent validations (10 per user)
        var sw = Stopwatch.StartNew();
        var results = await Task.WhenAll(
            Enumerable.Range(0, 1000).Select(i =>
            {
                var (userId, token) = tokenData[i % userCount];
                return _loginService.ValidateToken(userId, token, "Web");
            })
        );
        sw.Stop();

        var allValid = results.All(r => r);
        var p99Ms = sw.ElapsedMilliseconds; // Total / 1000 = average, P99 not directly measurable here

        await TestContext.Out.WriteLineAsync(
            $"1000 concurrent validations in {sw.ElapsedMilliseconds}ms, all passed: {allValid}");

        Assert.That(allValid, Is.True, "All valid tokens should pass validation under concurrent load");
        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(5000),
            "1000 concurrent validations should complete in under 5 seconds");
    }

    [Test]
    public async Task ConcurrentLogins_200DifferentUsers_AllReceiveTokens()
    {
        const int userCount = 200;
        var users = new List<UserModel>();

        for (int i = 0; i < userCount; i++)
        {
            var u = new UserModel
            {
                Name = $"Login{i}", Email = $"login{i}@stress.com", Password = "hash".StringToHash(), IsActive = true
            };
            await _userRepo.CreateAsync(u);
            users.Add(u);
        }

        // All users log in concurrently
        var sw = Stopwatch.StartNew();
        var tokenDtos = await Task.WhenAll(users.Select(u =>
            _loginService.Login(u.Email, "hash", "Web")));
        sw.Stop();

        await TestContext.Out.WriteLineAsync(
            $"200 concurrent logins in {sw.ElapsedMilliseconds}ms");

        Assert.That(tokenDtos, Has.All.Not.Null);
        Assert.That(tokenDtos.Select(t => t.AccessToken).Distinct().Count(), Is.EqualTo(userCount),
            "Each user should receive a unique access token");

        // Verify Redis has correct number of access token keys
        var db = _redis.GetDatabase();
        int foundKeys = 0;
        foreach (var user in users)
        {
            var key = CacheKeys.UserAccessToken(user.Id, "Web");
            if (await db.KeyExistsAsync(key)) foundKeys++;
        }

        Assert.That(foundKeys, Is.EqualTo(userCount),
            "Each login should store an access token in Redis");
    }

    [Test]
    public async Task TokenBlacklist_WriteAndConcurrentRead_100PercentHitRate()
    {
        // Arrange: log in users, then log them out to blacklist tokens
        const int userCount = 50;
        var loggedOutUsers = new List<(string userId, string token)>();

        for (int i = 0; i < userCount; i++)
        {
            var u = new UserModel
                { Name = $"BL{i}", Email = $"bl{i}@stress.com", Password = "hash".StringToHash(), IsActive = true };
            await _userRepo.CreateAsync(u);
            var tokenDto = await _loginService.Login(u.Email, "hash", "Web");
            await _loginService.Logout(u.Id, "Web");
            loggedOutUsers.Add((u.Id, tokenDto.AccessToken));
        }

        // Concurrently validate all blacklisted tokens
        var results = await Task.WhenAll(
            loggedOutUsers.Select(x => _loginService.ValidateToken(x.userId, x.token, "Web"))
        );

        var hitRate = results.Count(r => !r) * 100.0 / results.Length;
        await TestContext.Out.WriteLineAsync(
            $"Blacklist hit rate: {hitRate:F1}% ({results.Count(r => !r)}/{results.Length} rejected)");

        Assert.That(results, Has.All.False,
            "All logged-out users' tokens should be rejected (blacklist hit rate 100%)");
    }

    [Test]
    public async Task ExpiredTokens_BatchValidation_AllRejected()
    {
        // Simulate expired tokens: tokens stored in Redis with very short TTL
        var db = _redis.GetDatabase();
        const int count = 50;

        for (int i = 0; i < count; i++)
        {
            // Manually store tokens with 1ms TTL to simulate expiry
            await db.StringSetAsync(CacheKeys.UserAccessToken($"user-{i}", "Web"),
                $"expired-token-{i}", TimeSpan.FromMilliseconds(1));
        }

        await Task.Delay(50); // Let them expire (increased delay slightly for reliability)

        // Validate — all should fail because Redis keys are gone
        var results = await Task.WhenAll(
            Enumerable.Range(0, count).Select(i =>
                _loginService.ValidateToken($"user-{i}", $"expired-token-{i}", "Web"))
        );

        Assert.That(results, Has.All.False,
            "Expired tokens (TTL elapsed) should all be rejected");
    }

    [Test]
    public async Task RefreshToken_MultipleSimultaneousRefreshes_AllSucceed()
    {
        // Create a user and log in from multiple devices simultaneously
        var user = new UserModel
            { Name = "MultiRefresh", Email = "refresh@stress.com", Password = "hash".StringToHash(), IsActive = true };
        await _userRepo.CreateAsync(user);

        // Create 10 refresh tokens for the user
        const int deviceCount = 10;
        var refreshTokens = new List<string>();
        for (int i = 0; i < deviceCount; i++)
        {
            var tokenDto = await _loginService.Login(user.Email, "hash", $"Web");
            var db = _redis.GetDatabase();
            // Store unique refresh token per device
            var refreshToken = $"refresh-stress-{i}";
            await db.StringSetAsync(CacheKeys.UserRefreshToken(refreshToken), user.Id, TimeSpan.FromDays(3));
            refreshTokens.Add(refreshToken);
        }

        // Concurrently refresh all tokens
        var newTokens = await Task.WhenAll(
            refreshTokens.Select(rt => _loginService.Login(rt, "Web"))
        );

        Assert.That(newTokens, Has.All.Not.Null);
        var nonEmpty = newTokens.Count(t => !string.IsNullOrEmpty(t?.AccessToken));
        await TestContext.Out.WriteLineAsync(
            $"Successful concurrent refreshes: {nonEmpty}/{deviceCount}");
        Assert.That(nonEmpty, Is.EqualTo(deviceCount),
            "All concurrent refresh operations should succeed");
    }

    private class TestDbContextFactory(DbContextOptions<MarketContext> options) : IDbContextFactory<MarketContext>
    {
        public MarketContext CreateDbContext() => new MarketContext(options);
    }
}