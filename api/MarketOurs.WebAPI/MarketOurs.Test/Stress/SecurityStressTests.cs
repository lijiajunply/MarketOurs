using MarketOurs.DataAPI.Configs;
using MarketOurs.WebAPI.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketOurs.Test.Stress;

[TestFixture]
[Category("Security")]
public class SecurityStressTests
{
    private RateLimitService _rateLimitService;
    private Mock<ILogger<RateLimitService>> _mockLogger;

    [SetUp]
    public void Setup()
    {
        var config = new RateLimitConfig { EnableRedis = false }; // Disable Redis for unit tests
        _mockLogger = new Mock<ILogger<RateLimitService>>();
        _rateLimitService = new RateLimitService(config, _mockLogger.Object, Enumerable.Empty<StackExchange.Redis.IConnectionMultiplexer>());
    }

    [TearDown]
    public void TearDown()
    {
        _rateLimitService.Dispose();
    }

    [Test]
    public async Task LoginBruteForce_ShouldCorrectlyBlockAfterLimit()
    {
        // Arrange
        const string clientIp = "192.168.1.1";
        const string loginPath = "/auth/login";
        var policy = _rateLimitService.GetMatchingPolicy(loginPath);
        
        // Assert policy is indeed the auth policy (renamed from login to auth in new config)
        Assert.That(policy.Name, Is.EqualTo("auth"));
        int limit = policy.PermitLimit; // Should be 30 based on new RateLimitConfig.cs

        // Act & Assert
        // First 30 requests should succeed
        for (int i = 0; i < limit; i++)
        {
            var result = await _rateLimitService.CheckRateLimitAsync(loginPath, clientIp);
            Assert.That(result.IsAllowed, Is.True, $"Request {i+1} should have been allowed.");
        }

        // The 31st request should be blocked
        var blockedResult = await _rateLimitService.CheckRateLimitAsync(loginPath, clientIp);
        Assert.That(blockedResult.IsAllowed, Is.False, "Request 31 should have been blocked.");
    }

    [Test]
    public async Task RateLimit_HighIpCardinality_ShouldHandle10000UniqueIps()
    {
        // Arrange
        const int numUniqueIps = 10000;
        const string path = "/api/any";

        // Act
        var tasks = new List<Task<RateLimitResult>>();
        for (int i = 0; i < numUniqueIps; i++)
        {
            var ip = $"10.0.{i / 256}.{i % 256}";
            tasks.Add(_rateLimitService.CheckRateLimitAsync(path, ip));
        }

        // Assert
        var results = await Task.WhenAll(tasks);
        foreach (var result in results)
        {
            Assert.That(result.IsAllowed, Is.True);
        }
        
        await TestContext.Out.WriteLineAsync($"Successfully handled {numUniqueIps} unique IP limiters.");
    }
}