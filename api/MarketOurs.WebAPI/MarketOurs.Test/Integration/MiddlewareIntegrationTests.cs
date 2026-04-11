using System.Net;
using System.Text;
using System.Text.Json;
using MarketOurs.DataAPI.Configs;
using MarketOurs.DataAPI.Exceptions;
using MarketOurs.WebAPI.Middlewares;
using MarketOurs.WebAPI.Controllers;
using MarketOurs.WebAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;

namespace MarketOurs.Test.Integration;

[TestFixture]
[Category("Integration")]
public class MiddlewareIntegrationTests : IntegrationTestBase
{
    private ServiceProvider _serviceProvider;
    private IConnectionMultiplexer _redis;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();

        // 1. Setup Redis
        _redis = CreateRedisConnection();
        _redis.GetDatabase().Execute("FLUSHDB");
        services.AddSingleton(_redis);
        services.AddSingleton<IEnumerable<IConnectionMultiplexer>>([_redis]);

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = TestAssemblySetup.RedisConnectionString;
        });
        services.AddMemoryCache();

        // 2. Setup Middleware Dependencies
        services.AddSingleton<RateLimitConfig>();
        services.AddSingleton<RateLimitService>();
        services.AddScoped<IIpBlacklistCacheService, IpBlacklistCacheService>();
        services.AddSingleton<MaskingConfig>();
        services.AddSingleton<DataMaskingService>();

        var configDict = new Dictionary<string, string?>
        {
            { "IpBlacklist:Enabled", "true" }
        };
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(configDict).Build());

        // 3. Logging
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        _serviceProvider = services.BuildServiceProvider();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _serviceProvider.DisposeAsync();
        _redis.Dispose();
    }

    [Test]
    public async Task RateLimitMiddleware_ShouldThrottleAfterLimit()
    {
        // Arrange
        var rateLimitService = _serviceProvider.GetRequiredService<RateLimitService>();
        var blacklistService = _serviceProvider.GetRequiredService<IIpBlacklistCacheService>();
        var logger = _serviceProvider.GetRequiredService<ILogger<RateLimitMiddleware>>();

        int nextCalledCount = 0;

        var middleware = new RateLimitMiddleware(Next, logger, rateLimitService, blacklistService);

        var ip = "1.2.3.4";
        var context = new DefaultHttpContext
        {
            Connection =
            {
                RemoteIpAddress = IPAddress.Parse(ip)
            },
            Request =
            {
                Path = "/api/test"
            }
        };

        // Limit is 100 per minute by default config
        for (int i = 0; i < 100; i++)
        {
            await middleware.InvokeAsync(context);
            context.Response.StatusCode = 200; // Reset for next iteration if needed
        }

        Assert.That(nextCalledCount, Is.EqualTo(100));

        // 201st request should be throttled
        var throttledContext = new DefaultHttpContext
        {
            Connection =
            {
                RemoteIpAddress = IPAddress.Parse(ip)
            },
            Request =
            {
                Path = "/api/test"
            },
            Response =
            {
                // Use a real stream for the response body because WriteAsJsonAsync needs it
                Body = new MemoryStream()
            }
        };

        await middleware.InvokeAsync(throttledContext);

        // Assert
        Assert.That(throttledContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status429TooManyRequests));
        Assert.That(nextCalledCount, Is.EqualTo(100), "Next should not be called when throttled");
        return;

        Task Next(HttpContext ctx)
        {
            nextCalledCount++;
            return Task.CompletedTask;
        }
    }

    [Test]
    public async Task RateLimitMiddleware_BlacklistedIp_ShouldRejectImmediately()
    {
        // Arrange
        var rateLimitService = _serviceProvider.GetRequiredService<RateLimitService>();
        var blacklistService = _serviceProvider.GetRequiredService<IIpBlacklistCacheService>();
        var logger = _serviceProvider.GetRequiredService<ILogger<RateLimitMiddleware>>();

        var middleware = new RateLimitMiddleware(Next, logger, rateLimitService, blacklistService);

        var badIp = "9.9.9.9";
        await blacklistService.AddToBlacklistAsync(badIp);
        // Force refresh to memory cache
        await blacklistService.RefreshBlacklistAsync();

        var context = new DefaultHttpContext
        {
            Connection =
            {
                RemoteIpAddress = IPAddress.Parse(badIp)
            },
            Response =
            {
                Body = new MemoryStream()
            }
        };

        // Act
        await middleware.InvokeAsync(context);

        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status429TooManyRequests));
        return;

        Task Next(HttpContext ctx) => Task.CompletedTask;
    }

    [Test]
    public async Task DataMaskingMiddleware_ShouldMaskSensitiveDataInJson()
    {
        // Arrange
        var maskingService = _serviceProvider.GetRequiredService<DataMaskingService>();
        var logger = _serviceProvider.GetRequiredService<ILogger<DataMaskingMiddleware>>();

        var middleware = new DataMaskingMiddleware(Next, logger, maskingService);

        var context = new DefaultHttpContext
        {
            Request =
            {
                Path = "/api/users/profile"
            }
        };
        var originalBody = new MemoryStream();
        context.Response.Body = originalBody;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        originalBody.Seek(0, SeekOrigin.Begin);
        var result = await new StreamReader(originalBody).ReadToEndAsync();

        // Check if masked (exact masking pattern depends on implementation, but it shouldn't be the original)
        Assert.That(result, Does.Not.Contain("secret@example.com"));
        Assert.That(result, Does.Not.Contain("13800138000"));
        Assert.That(result, Does.Contain("****"));
        return;

        async Task Next(HttpContext ctx)
        {
            ctx.Response.ContentType = "application/json";
            var json = "{\"email\": \"secret@example.com\", \"phone\": \"13800138000\"}";
            var bytes = Encoding.UTF8.GetBytes(json);
            await ctx.Response.Body.WriteAsync(bytes, 0, bytes.Length);
        }
    }

    [Test]
    public async Task GlobalExceptionMiddleware_ShouldConvertExceptionToApiResponse()
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<GlobalExceptionMiddleware>>();
        var middleware = new GlobalExceptionMiddleware(Next, logger);

        var context = new DefaultHttpContext();
        var bodyStream = new MemoryStream();
        context.Response.Body = bodyStream;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));

        bodyStream.Seek(0, SeekOrigin.Begin);
        var responseJson = await new StreamReader(bodyStream).ReadToEndAsync();
        Assert.That(responseJson, Does.Contain("\"code\":500"));
        Assert.That(responseJson, Does.Contain("Unexpected server error"));
        return;

        // Arrange
        Task Next(HttpContext ctx) => throw new Exception("Unexpected server error");
    }

    [Test]
    public async Task RateLimitMiddleware_IpUnblockedAfterRemoval_AllowsRequests()
    {
        // Arrange: blacklist an IP then remove it
        var rateLimitService = _serviceProvider.GetRequiredService<RateLimitService>();
        var blacklistService = _serviceProvider.GetRequiredService<IIpBlacklistCacheService>();
        var logger = _serviceProvider.GetRequiredService<ILogger<RateLimitMiddleware>>();
        int nextCalled = 0;
        var middleware = new RateLimitMiddleware(Next, logger, rateLimitService, blacklistService);

        var ip = "7.7.7.7";
        await blacklistService.AddToBlacklistAsync(ip);
        await blacklistService.RefreshBlacklistAsync();

        // Confirm blocked
        var blockedCtx = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = System.Net.IPAddress.Parse(ip) },
            Response = { Body = new MemoryStream() }
        };
        await middleware.InvokeAsync(blockedCtx);
        Assert.That(blockedCtx.Response.StatusCode, Is.EqualTo(StatusCodes.Status429TooManyRequests));

        // Remove from blacklist and refresh
        await blacklistService.RemoveFromBlacklistAsync(ip);
        await blacklistService.RefreshBlacklistAsync();

        // Should now be allowed through
        var allowedCtx = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = System.Net.IPAddress.Parse(ip) },
            Request = { Path = "/api/ok" },
            Response = { Body = new MemoryStream() }
        };
        await middleware.InvokeAsync(allowedCtx);
        Assert.That(nextCalled, Is.EqualTo(1), "Request should pass after IP is removed from blacklist");
        return;

        Task Next(HttpContext ctx) { Interlocked.Increment(ref nextCalled); return Task.CompletedTask; }
    }

    [Test]
    public async Task DataMaskingMiddleware_NestedFields_MasksCorrectly()
    {
        // Arrange
        var maskingService = _serviceProvider.GetRequiredService<DataMaskingService>();
        var logger = _serviceProvider.GetRequiredService<ILogger<DataMaskingMiddleware>>();
        var middleware = new DataMaskingMiddleware(Next, logger, maskingService);

        var context = new DefaultHttpContext { Request = { Path = "/api/users/profile" } };
        var originalBody = new MemoryStream();
        context.Response.Body = originalBody;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        originalBody.Seek(0, SeekOrigin.Begin);
        var result = await new StreamReader(originalBody).ReadToEndAsync();

        Assert.That(result, Does.Not.Contain("inner@example.com"));
        Assert.That(result, Does.Contain("****"));
        return;

        async Task Next(HttpContext ctx)
        {
            ctx.Response.ContentType = "application/json";
            var json = "{\"user\":{\"email\": \"inner@example.com\", \"phone\": \"13911112222\"}}";
            var bytes = Encoding.UTF8.GetBytes(json);
            await ctx.Response.Body.WriteAsync(bytes, 0, bytes.Length);
        }
    }

    [Test]
    public async Task GlobalExceptionMiddleware_AuthException_Returns401()
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<GlobalExceptionMiddleware>>();
        var middleware = new GlobalExceptionMiddleware(Next, logger);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var response = JsonSerializer.Deserialize<ApiResponse<object>>(await new StreamReader(context.Response.Body).ReadToEndAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.ErrorCode, Is.EqualTo(ErrorCode.Unauthorized));
        Assert.That(response.Code, Is.EqualTo(StatusCodes.Status401Unauthorized));
        return;

        Task Next(HttpContext ctx) =>
            throw new MarketOurs.DataAPI.Exceptions.AuthException(
                ErrorCode.Unauthorized, "Not authorized");
    }

    [Test]
    public async Task GlobalExceptionMiddleware_KeyNotFoundException_Returns404()
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<GlobalExceptionMiddleware>>();
        var middleware = new GlobalExceptionMiddleware(Next, logger);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var response = JsonSerializer.Deserialize<ApiResponse<object>>(await new StreamReader(context.Response.Body).ReadToEndAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.ErrorCode, Is.EqualTo(ErrorCode.ResourceNotFound));
        return;

        Task Next(HttpContext ctx) => throw new KeyNotFoundException("Resource not found");
    }

    [Test]
    public async Task GlobalExceptionMiddleware_ArgumentNullException_Returns400()
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<GlobalExceptionMiddleware>>();
        var middleware = new GlobalExceptionMiddleware(Next, logger);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var response = JsonSerializer.Deserialize<ApiResponse<object>>(await new StreamReader(context.Response.Body).ReadToEndAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.ErrorCode, Is.EqualTo(ErrorCode.ParameterEmpty));
        return;

        Task Next(HttpContext ctx) => throw new ArgumentNullException("userId");
    }

    [Test]
    public async Task RateLimitMiddleware_HighConcurrency_ShouldWorkCorrectly()
    {
        // Arrange
        var rateLimitService = _serviceProvider.GetRequiredService<RateLimitService>();

        // Mock blacklist service to avoid semaphore bottleneck in high concurrency test
        var mockBlacklist = new Mock<IIpBlacklistCacheService>();
        mockBlacklist.Setup(x => x.IsIpBlacklistedAsync(It.IsAny<string>())).ReturnsAsync(false);

        var logger = _serviceProvider.GetRequiredService<ILogger<RateLimitMiddleware>>();

        int nextCalledCount = 0;

        var middleware = new RateLimitMiddleware(Next, logger, rateLimitService, mockBlacklist.Object);
        var ip = "1.1.1.1";

        // Limit is 100. Let's send 150 concurrent requests.
        var tasks = Enumerable.Range(0, 150).Select(async _ =>
        {
            var context = new DefaultHttpContext
            {
                Connection =
                {
                    RemoteIpAddress = IPAddress.Parse(ip)
                },
                Request =
                {
                    Path = "/api/stress"
                },
                Response =
                {
                    Body = new MemoryStream()
                }
            };
            await middleware.InvokeAsync(context);
            return context.Response.StatusCode;
        });

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        var successCount = results.Count(s => s != StatusCodes.Status429TooManyRequests);
        var throttledCount = results.Count(s => s == StatusCodes.Status429TooManyRequests);

        // Success count should be exactly 100 (the limit)
        Assert.That(successCount, Is.EqualTo(100), "Should allow exactly 100 requests");
        Assert.That(throttledCount, Is.EqualTo(50), "Should throttle 50 requests");
        Assert.That(nextCalledCount, Is.EqualTo(100));
        return;

        Task Next(HttpContext ctx)
        {
            Interlocked.Increment(ref nextCalledCount);
            return Task.CompletedTask;
        }
    }
}
