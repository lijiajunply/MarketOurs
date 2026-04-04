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
        var config = new RateLimitConfig();
        _mockLogger = new Mock<ILogger<RateLimitService>>();
        _rateLimitService = new RateLimitService(config, _mockLogger.Object);
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
        
        // Assert policy is indeed the login policy
        Assert.That(policy.Name, Is.EqualTo("login"));
        int limit = policy.TokenLimit; // Should be 30 based on RateLimitConfig.cs

        // Act & Assert
        // First 30 requests should succeed
        for (int i = 0; i < limit; i++)
        {
            var lease = await _rateLimitService.TryAcquireTokenAsync(policy, clientIp);
            Assert.That(lease.IsAcquired, Is.True, $"Request {i+1} should have been allowed.");
            lease.Dispose();
        }

        // The 31st request should be blocked
        var blockedLease = await _rateLimitService.TryAcquireTokenAsync(policy, clientIp);
        Assert.That(blockedLease.IsAcquired, Is.False, "Request 31 should have been blocked.");
        blockedLease.Dispose();
    }

    [Test]
    public async Task RateLimit_HighIpCardinality_ShouldHandle10000UniqueIps()
    {
        // Arrange
        const int numUniqueIps = 10000;
        const string path = "/api/any";
        var policy = _rateLimitService.GetMatchingPolicy(path);

        // Act
        var tasks = new List<ValueTask<System.Threading.RateLimiting.RateLimitLease>>();
        for (int i = 0; i < numUniqueIps; i++)
        {
            var ip = $"10.0.{i / 256}.{i % 256}";
            tasks.Add(_rateLimitService.TryAcquireTokenAsync(policy, ip));
        }

        // Assert
        foreach (var task in tasks)
        {
            using var lease = await task;
            Assert.That(lease.IsAcquired, Is.True);
        }
        
        await TestContext.Out.WriteLineAsync($"Successfully handled {numUniqueIps} unique IP limiters.");
    }
}