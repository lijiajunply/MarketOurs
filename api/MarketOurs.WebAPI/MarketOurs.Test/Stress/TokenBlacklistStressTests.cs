using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Services;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace MarketOurs.Test.Stress;

[TestFixture]
[Category("HighLoad")]
public class TokenBlacklistStressTests
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
    public void Setup()
    {
        _mockUserService = new Mock<IUserService>();
        _mockJwtService = new Mock<IJwtService>();
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockDatabase = new Mock<IDatabase>();
        _mockLogger = new Mock<ILogger<LoginService>>();
        _mockEmailService = new Mock<IEmailService>();
        _mockSmsService = new Mock<ISmsService>();

        _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockDatabase.Object);

        // Return true for all StringSetAsync calls
        _mockDatabase.SetReturnsDefault(Task.FromResult(true));
        // Return a dummy value for StringGetAsync
        _mockDatabase.SetReturnsDefault(Task.FromResult(RedisValue.EmptyString));

        var redisList = new List<IConnectionMultiplexer> { _mockRedis.Object };
        _loginService = new LoginService(
            _mockUserService.Object,
            redisList,
            _mockJwtService.Object,
            _mockEmailService.Object,
            _mockSmsService.Object,
            _mockLogger.Object
        );
    }

    [Test]
    public async Task LoginAndLogout_HighConcurrency_ShouldBeStable()
    {
        // Arrange
        const int numUsers = 1000;
        var users = Enumerable.Range(0, numUsers).Select(i => new UserDto { Id = $"u_{i}", IsActive = true }).ToList();

        _mockJwtService.Setup(j => j.GetAccessToken(It.IsAny<UserDto>(), It.IsAny<DeviceType>())).ReturnsAsync("token");
        _mockJwtService.Setup(j => j.GetRefreshToken(It.IsAny<DeviceType>())).ReturnsAsync("refresh");
        _mockUserService.Setup(s => s.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string id, string _) => new UserDto { Id = id, IsActive = true });

        // Act & Assert - Bulk Login
        await Parallel.ForEachAsync(users, async (user, _) => { await _loginService.Login(user.Id, "pass", "Web"); });

        // Act & Assert - Bulk Logout
        await Parallel.ForEachAsync(users, async (user, _) => { await _loginService.Logout(user.Id, "Web"); });

        // If we reached here without exception, the concurrent state management is stable
        _mockDatabase.Verify(db => db.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()),
            Times.Exactly(numUsers));
    }
}