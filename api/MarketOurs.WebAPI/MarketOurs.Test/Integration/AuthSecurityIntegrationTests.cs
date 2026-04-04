using MarketOurs.DataAPI.Configs;
using MarketOurs.Data;
using MarketOurs.Data.DataModels;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services;
using MarketOurs.DataAPI.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;
using Microsoft.Extensions.Options;

namespace MarketOurs.Test.Integration;

[TestFixture]
[Category("Integration")]
public class AuthSecurityIntegrationTests : IntegrationTestBase
{
    private ServiceProvider _serviceProvider;
    private ILoginService _loginService;
    private IUserService _userService;
    private IConnectionMultiplexer _redis;
    private IDatabase _db;

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

        // 2. Setup Redis
        _redis = CreateRedisConnection();
        _db = _redis.GetDatabase();
        _db.Execute("FLUSHDB");
        services.AddSingleton<IConnectionMultiplexer>(_redis);
        services.AddSingleton<IEnumerable<IConnectionMultiplexer>>(new[] { _redis });

        // 3. Setup JWT & Configs
        services.AddScoped<IUserService, UserService>();
        
        // Ensure RSA keys exist or mock JwtService to avoid missing file exceptions
        var mockJwtService = new Moq.Mock<IJwtService>();
        mockJwtService.Setup(x => x.GetAccessToken(It.IsAny<UserDto>(), It.IsAny<DeviceType>())).ReturnsAsync("mocked_access_token");
        mockJwtService.Setup(x => x.GetRefreshToken(It.IsAny<DeviceType>())).ReturnsAsync("mocked_refresh_token");
        mockJwtService.Setup(x => x.ValidateAccessToken(It.IsAny<string>())).Returns((true, new System.Security.Claims.Claim[0]));

        var mockEmailService = new Moq.Mock<IEmailService>();
        services.AddSingleton<IEmailService>(mockEmailService.Object);

        services.AddSingleton<IJwtService>(mockJwtService.Object);
        services.AddScoped<ILoginService, LoginService>();
        
        // 4. Logging
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        _serviceProvider = services.BuildServiceProvider();
        _loginService = _serviceProvider.GetRequiredService<ILoginService>();
        _userService = _serviceProvider.GetRequiredService<IUserService>();

        // Clear DB
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IDbContextFactory<MarketContext>>().CreateDbContext();
        context.Database.ExecuteSqlRaw("TRUNCATE TABLE \"users\" CASCADE");
    }

    [TearDown]
    public async Task TearDown()
    {
        await _serviceProvider.DisposeAsync();
        _redis.Dispose();
    }

    [Test]
    public async Task Login_ValidUser_ShouldReturnTokensAndStoreInRedis()
    {
        // Arrange
        var password = "SecurePassword123";
        var createDto = new UserCreateDto
        {
            Account = "testuser@test.com",
            Password = password,
            Name = "Test User"
        };
        
        var userDto = await _userService.CreateAsync(createDto);

        // Act
        var tokenDto = await _loginService.Login("testuser@test.com", password, "PC");

        // Assert
        Assert.That(tokenDto, Is.Not.Null);
        Assert.That(tokenDto.AccessToken, Is.Not.Empty);
        Assert.That(tokenDto.RefreshToken, Is.Not.Empty);

        // Verify Redis Storage
        var accessKey = CacheKeys.UserAccessToken(userDto.Id, "PC");
        var storedAccessToken = await _db.StringGetAsync(accessKey);
        
        var refreshKey = CacheKeys.UserRefreshToken(tokenDto.RefreshToken);
        var storedUserId = await _db.StringGetAsync(refreshKey);

        Assert.That(storedAccessToken.ToString(), Is.EqualTo(tokenDto.AccessToken));
        Assert.That(storedUserId.ToString(), Is.EqualTo(userDto.Id));
    }

    [Test]
    public void Login_InvalidUser_ShouldThrowAuthException()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<AuthException>(async () => 
            await _loginService.Login("nonexistent@test.com", "wrongpass", "PC"));
        
        Assert.That(ex.ErrorCode, Is.EqualTo(ErrorCode.UserNotFound));
    }

    [Test]
    public async Task Logout_ShouldRemoveTokenFromRedis()
    {
        // Arrange
        var password = "SecurePassword123";
        var createDto = new UserCreateDto { Account = "logoutuser@test.com", Password = password, Name = "User" };
        var userDto = await _userService.CreateAsync(createDto);
        var tokenDto = await _loginService.Login("logoutuser@test.com", password, "PC");

        var accessKey = CacheKeys.UserAccessToken(userDto.Id, "PC");
        Assert.That((await _db.KeyExistsAsync(accessKey)), Is.True);

        // Act
        var result = await _loginService.Logout(userDto.Id, "PC");

        // Assert
        Assert.That(result, Is.True);
        Assert.That((await _db.KeyExistsAsync(accessKey)), Is.False);
    }
    
    [Test]
    public async Task ValidateToken_WhenRedisHasToken_ShouldReturnTrue()
    {
        // Arrange
        var password = "SecurePassword123";
        var createDto = new UserCreateDto { Account = "valid@test.com", Password = password, Name = "User" };
        var userDto = await _userService.CreateAsync(createDto);
        var tokenDto = await _loginService.Login("valid@test.com", password, "PC");

        // Act
        var isValid = await _loginService.ValidateToken(userDto.Id, tokenDto.AccessToken, "PC");
        var isInvalid = await _loginService.ValidateToken(userDto.Id, "wrong_token", "PC");

        // Assert
        Assert.That(isValid, Is.True);
        Assert.That(isInvalid, Is.False);
    }

    private class TestDbContextFactory(DbContextOptions<MarketContext> options) : IDbContextFactory<MarketContext>
    {
        public MarketContext CreateDbContext() => new MarketContext(options);
    }
}