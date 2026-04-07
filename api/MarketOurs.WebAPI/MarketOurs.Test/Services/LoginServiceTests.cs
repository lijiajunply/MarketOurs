using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Configs;
using MarketOurs.DataAPI.Exceptions;
using MarketOurs.DataAPI.Services;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace MarketOurs.Test.Services;

[TestFixture]
public class LoginServiceTests
{
    private Mock<IUserService> _mockUserService;
    private Mock<IJwtService> _mockJwtService;
    private Mock<IConnectionMultiplexer> _mockRedis;
    private Mock<IDatabase> _mockDatabase;
    private Mock<ILogger<LoginService>> _mockLogger;
    private LoginService _loginService;
    private Mock<ISmsService> _mockSmsService;
    private Mock<IEmailService> _mockEmailService;

    [SetUp]
    public void SetUp()
    {
        _mockUserService = new Mock<IUserService>();
        _mockJwtService = new Mock<IJwtService>();
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockDatabase = new Mock<IDatabase>();
        _mockLogger = new Mock<ILogger<LoginService>>();
        _mockEmailService = new Mock<IEmailService>();
        _mockSmsService = new Mock<ISmsService>();

        _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockDatabase.Object);
        var redisList = new List<IConnectionMultiplexer> { _mockRedis.Object };

        _loginService = new LoginService(
            _mockUserService.Object,
            redisList,
            _mockJwtService.Object,
            _mockEmailService.Object,
            _mockSmsService.Object,
            new SmsConfig(),
            _mockLogger.Object
        );
    }

    [Test]
    public async Task Login_WithValidCredentials_ShouldReturnTokenDto()
    {
        // Arrange
        var user = new UserDto { Id = "1", Name = "Test User" };
        _mockUserService.Setup(s => s.LoginAsync("test", "password")).ReturnsAsync(user);
        _mockJwtService.Setup(j => j.GetAccessToken(user, It.IsAny<DeviceType>())).ReturnsAsync("access_token");
        _mockJwtService.Setup(j => j.GetRefreshToken(It.IsAny<DeviceType>())).ReturnsAsync("refresh_token");

        // Act
        var result = await _loginService.Login("test", "password", "Web");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.AccessToken, Is.EqualTo("access_token"));
        Assert.That(result.RefreshToken, Is.EqualTo("refresh_token"));
    }

    [Test]
    public void Login_WithInvalidCredentials_ShouldThrowAuthException()
    {
        // Arrange
        _mockUserService.Setup(s => s.LoginAsync("test", "wrong")).ReturnsAsync((UserDto?)null);

        // Act & Assert
        var ex = Assert.ThrowsAsync<AuthException>(async () => await _loginService.Login("test", "wrong", "Web"));
        Assert.That(ex.ErrorCode, Is.EqualTo(4003)); // ErrorCode.UserNotFound is 4003
    }

    [Test]
    public async Task LoginWithOAuthAsync_WhenUserExists_ShouldReturnTokens()
    {
        // Arrange
        var user = new UserDto { Id = "1", Name = "OAuth User", IsActive = true, GithubId = "github_id" };
        _mockUserService.Setup(s => s.GetByThirdPartyIdAsync("Github", "github_id")).ReturnsAsync(user);
        _mockJwtService.Setup(j => j.GetAccessToken(user, It.IsAny<DeviceType>())).ReturnsAsync("access_token");
        _mockJwtService.Setup(j => j.GetRefreshToken(It.IsAny<DeviceType>())).ReturnsAsync("refresh_token");

        // Act
        var result = await _loginService.LoginWithOAuthAsync("Github", "github_id", "oauth@test.com", "OAuth User", "avatar_url", "Web");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.AccessToken, Is.EqualTo("access_token"));
    }

    [Test]
    public async Task LoginWithOAuthAsync_WhenUserDoesNotExist_AndIsOurs_ShouldCreateUserAndReturnTokens()
    {
        // Arrange
        var newUser = new UserDto { Id = "2", Name = "New User", IsActive = true, OursId = "ours_id" };
        _mockUserService.Setup(s => s.GetByThirdPartyIdAsync("Ours", "ours_id")).ReturnsAsync((UserDto?)null);
        _mockUserService.Setup(s => s.GetByAccountAsync("new@test.com")).ReturnsAsync((UserDto?)null);
        _mockUserService.Setup(s => s.CreateAsync(It.IsAny<UserCreateDto>())).ReturnsAsync(newUser);
        _mockUserService.Setup(s => s.GetByIdAsync("2")).ReturnsAsync(newUser);
        _mockJwtService.Setup(j => j.GetAccessToken(newUser, It.IsAny<DeviceType>())).ReturnsAsync("access_token");
        _mockJwtService.Setup(j => j.GetRefreshToken(It.IsAny<DeviceType>())).ReturnsAsync("refresh_token");

        // Act
        var result = await _loginService.LoginWithOAuthAsync("Ours", "ours_id", "new@test.com", "New User", "avatar_url", "Web");

        // Assert
        Assert.That(result, Is.Not.Null);
        _mockUserService.Verify(s => s.CreateAsync(It.Is<UserCreateDto>(d => d.Account == "new@test.com")), Times.Once);
    }

    [Test]
    public async Task Login_WithRefreshToken_ShouldReturnNewTokens()
    {
        // Arrange
        var user = new UserDto { Id = "1", Name = "User 1", IsActive = true };
        _mockDatabase.Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue("1"));
        _mockUserService.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(user);
        _mockJwtService.Setup(j => j.GetAccessToken(user, It.IsAny<DeviceType>())).ReturnsAsync("new_access_token");
        _mockJwtService.Setup(j => j.GetRefreshToken(It.IsAny<DeviceType>())).ReturnsAsync("new_refresh_token");

        // Act
        var result = await _loginService.Login("valid_refresh_token", "Web");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.AccessToken, Is.EqualTo("new_access_token"));
    }

    [Test]
    public async Task Logout_ShouldDeleteKeyInRedis()
    {
        // Arrange
        _mockDatabase.Setup(db => db.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);

        // Act
        var result = await _loginService.Logout("1", "Web");

        // Assert
        Assert.That(result, Is.True);
        _mockDatabase.Verify(db => db.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Once);
    }
}