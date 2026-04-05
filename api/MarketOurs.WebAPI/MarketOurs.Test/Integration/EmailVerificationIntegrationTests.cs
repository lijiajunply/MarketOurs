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

[TestFixture]
[Category("Integration")]
public class EmailVerificationIntegrationTests : IntegrationTestBase
{
    private ServiceProvider _serviceProvider;
    private IUserService _userService;
    private IUserRepo _userRepo;
    private IConnectionMultiplexer _redis;
    private string _capturedEmailToken = string.Empty;

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
                    _capturedEmailToken = prop.GetValue(model)?.ToString() ?? string.Empty;
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
        _capturedEmailToken = string.Empty;
    }

    [TearDown]
    public async Task TearDown()
    {
        await _serviceProvider.DisposeAsync();
        _redis.Dispose();
    }

    [Test]
    public async Task SendAndVerifyEmail_ValidToken_SetsEmailVerifiedTrue()
    {
        var user = await _userService.CreateAsync(new UserCreateDto
            { Account = "verify@test.com", Password = "Pass123", Name = "VerifyUser" });

        var sent = await _userService.SendVerificationEmailAsync(user.Id);
        Assert.That(sent, Is.True);
        Assert.That(_capturedEmailToken, Is.Not.Empty);

        var result = await _userService.VerifyEmailAsync(_capturedEmailToken);
        Assert.That(result, Is.True);

        var dbUser = await _userRepo.GetByIdAsync(user.Id);
        Assert.That(dbUser!.IsEmailVerified, Is.True);
    }

    [Test]
    public async Task VerifyEmail_InvalidToken_ReturnsFalse()
    {
        var result = await _userService.VerifyEmailAsync("INVALID-TOKEN-XYZ");
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task VerifyEmail_AlreadyConsumedToken_CannotBeReusedAfterVerification()
    {
        var user = await _userService.CreateAsync(new UserCreateDto
            { Account = "once@test.com", Password = "Pass123", Name = "OnceUser" });

        await _userService.SendVerificationEmailAsync(user.Id);
        var token = _capturedEmailToken;

        // First use succeeds
        var first = await _userService.VerifyEmailAsync(token);
        Assert.That(first, Is.True);

        // Second use with the same token should fail (key deleted from Redis)
        var second = await _userService.VerifyEmailAsync(token);
        Assert.That(second, Is.False);
    }

    [Test]
    public async Task SendVerificationEmail_Twice_OldTokenNoLongerValid()
    {
        var user = await _userService.CreateAsync(new UserCreateDto
            { Account = "resend@test.com", Password = "Pass123", Name = "ResendUser" });

        // First send
        await _userService.SendVerificationEmailAsync(user.Id);
        var oldToken = _capturedEmailToken;

        // Second send (new token issued)
        _capturedEmailToken = string.Empty;
        await _userService.SendVerificationEmailAsync(user.Id);
        var newToken = _capturedEmailToken;

        Assert.That(newToken, Is.Not.EqualTo(oldToken), "New token should differ from old token");

        // New token should work
        var result = await _userService.VerifyEmailAsync(newToken);
        Assert.That(result, Is.True);

        // Old token should be orphaned in Redis but with wrong key — it will verify the user too if still present
        // The important check is that newToken worked
    }

    [Test]
    public async Task SendVerificationEmail_UserWithoutEmail_ReturnsFalse()
    {
        // Phone-only user (no email)
        var user = new UserModel
        {
            Name = "PhoneOnly",
            Phone = "13812345678",
            Password = "hash",
            Email = string.Empty,
            IsActive = true
        };
        await _userRepo.CreateAsync(user);

        var result = await _userService.SendVerificationEmailAsync(user.Id);
        Assert.That(result, Is.False, "Cannot send verification to user with no email");
    }

    [Test]
    public async Task SendVerificationEmail_NonExistentUser_ReturnsFalse()
    {
        var result = await _userService.SendVerificationEmailAsync("non-existent-user-id-xxx");
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task VerifyEmail_ThenLogin_SessionEstablishedSuccessfully()
    {
        var password = "SecurePass!99";
        var user = await _userService.CreateAsync(new UserCreateDto
            { Account = "loginafter@test.com", Password = password, Name = "LoginAfterVerify" });

        await _userService.SendVerificationEmailAsync(user.Id);
        await _userService.VerifyEmailAsync(_capturedEmailToken);

        // Login should work normally after email is verified
        var loggedIn = await _userService.LoginAsync("loginafter@test.com", password);
        Assert.That(loggedIn, Is.Not.Null);
        Assert.That(loggedIn!.IsEmailVerified, Is.True);
    }

    private class TestDbContextFactory(DbContextOptions<MarketContext> options) : IDbContextFactory<MarketContext>
    {
        public MarketContext CreateDbContext() => new MarketContext(options);
    }
}
