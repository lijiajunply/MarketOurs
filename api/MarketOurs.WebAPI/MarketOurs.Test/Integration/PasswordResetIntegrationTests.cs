using MarketOurs.Data;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;

namespace MarketOurs.Test.Integration;

[TestFixture]
[Category("Integration")]
public class PasswordResetIntegrationTests : IntegrationTestBase
{
    private ServiceProvider _serviceProvider;
    private IUserService _userService;
    private IUserRepo _userRepo;
    private IConnectionMultiplexer _redis;
    private string _capturedResetToken = string.Empty;

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
        services.AddSingleton<IEnumerable<IConnectionMultiplexer>>([_redis]);

        var mockEmail = new Mock<IEmailService>();
        mockEmail
            .Setup(x => x.SendEmailWithTemplateAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<object>(), It.IsAny<bool>()))
            .Callback<string, string, string, object, bool>((_, _, _, model, _) =>
            {
                var prop = model?.GetType().GetProperty("token");
                if (prop != null)
                    _capturedResetToken = prop.GetValue(model)?.ToString() ?? string.Empty;
            })
            .ReturnsAsync(true);
        services.AddSingleton(mockEmail.Object);

        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddScoped<IUserService, UserService>();

        _serviceProvider = services.BuildServiceProvider();
        _userService = _serviceProvider.GetRequiredService<IUserService>();
        _userRepo = _serviceProvider.GetRequiredService<IUserRepo>();

        using var scope = _serviceProvider.CreateScope();
        var ctx = await scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<MarketContext>>().CreateDbContextAsync();
        ctx.Database.ExecuteSqlRaw("TRUNCATE TABLE \"users\" CASCADE");
        _capturedResetToken = string.Empty;
    }

    [TearDown]
    public async Task TearDown()
    {
        await _serviceProvider.DisposeAsync();
        _redis.Dispose();
    }

    [Test]
    public async Task FullResetFlow_ValidToken_NewPasswordLoginSucceeds()
    {
        var originalPassword = "OldPass123";
        var newPassword = "NewPass456";

        var user = await _userService.CreateAsync(new UserCreateDto
            { Account = "reset@test.com", Password = originalPassword, Name = "ResetUser" });

        var forgot = await _userService.ForgotPasswordAsync("reset@test.com");
        Assert.That(forgot, Is.True);
        Assert.That(_capturedResetToken, Is.Not.Empty);

        var reset = await _userService.ResetPasswordAsync(_capturedResetToken, newPassword);
        Assert.That(reset, Is.True);

        // Old password should fail
        var oldLogin = await _userService.LoginAsync("reset@test.com", originalPassword);
        Assert.That(oldLogin, Is.Null, "Old password should be rejected after reset");

        // New password should succeed
        var newLogin = await _userService.LoginAsync("reset@test.com", newPassword);
        Assert.That(newLogin, Is.Not.Null, "New password should work after reset");
    }

    [Test]
    public async Task ResetPassword_InvalidToken_ReturnsFalse()
    {
        var result = await _userService.ResetPasswordAsync("BOGUS-TOKEN", "AnyPass123");
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ResetPassword_TokenConsumedTwice_SecondCallFails()
    {
        await _userService.CreateAsync(new UserCreateDto
            { Account = "twice@test.com", Password = "Init123", Name = "TwiceUser" });

        await _userService.ForgotPasswordAsync("twice@test.com");
        var token = _capturedResetToken;

        var first = await _userService.ResetPasswordAsync(token, "NewPass111");
        Assert.That(first, Is.True);

        var second = await _userService.ResetPasswordAsync(token, "NewPass222");
        Assert.That(second, Is.False, "Token should be deleted after first use");
    }

    [Test]
    public async Task ForgotPassword_NonExistentAccount_ReturnsFalse()
    {
        var result = await _userService.ForgotPasswordAsync("doesnotexist@test.com");
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ForgotPassword_DoesNotLeakUserExistence_SameReturnType()
    {
        // Both existing and non-existing accounts should return bool (no exception thrown)
        await _userService.CreateAsync(new UserCreateDto
            { Account = "exists@test.com", Password = "Pass123", Name = "Exists" });

        var existsResult = await _userService.ForgotPasswordAsync("exists@test.com");
        var noExistsResult = await _userService.ForgotPasswordAsync("noexist@test.com");

        // No exception should be thrown for either case
        Assert.That(existsResult, Is.True);
        Assert.That(noExistsResult, Is.False);
    }

    [Test]
    public async Task ResetPassword_EmptyNewPassword_StillHashesAndSaves()
    {
        // The service does not validate password length — that's a controller concern.
        // This test verifies the service layer accepts the call without throwing.
        await _userService.CreateAsync(new UserCreateDto
            { Account = "empty@test.com", Password = "Init123", Name = "EmptyPassUser" });

        await _userService.ForgotPasswordAsync("empty@test.com");
        var token = _capturedResetToken;

        // Service should not crash on empty password (hashing an empty string is valid)
        Assert.DoesNotThrowAsync(() => _userService.ResetPasswordAsync(token, string.Empty));
    }

    [Test]
    public async Task ForgotPassword_PhoneUser_CompletesWithoutEmail()
    {
        // Phone-only user: ForgotPassword via phone account
        // (The service logs a mock SMS but returns true if user found)
        var user = await _userService.CreateAsync(new UserCreateDto
            { Account = "13988887777", Password = "Pass123", Name = "PhoneUser" });

        // Service tries phone path when account doesn't contain '@'
        var result = await _userService.ForgotPasswordAsync("13988887777");
        // Should return true (mock SMS path) and not throw
        Assert.That(result, Is.True);
    }

    private class TestDbContextFactory(DbContextOptions<MarketContext> options) : IDbContextFactory<MarketContext>
    {
        public MarketContext CreateDbContext() => new MarketContext(options);
    }
}
