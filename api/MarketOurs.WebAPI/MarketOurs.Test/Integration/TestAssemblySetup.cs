using MarketOurs.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Microsoft.Extensions.Logging;

namespace MarketOurs.Test.Integration;

[SetUpFixture]
public class TestAssemblySetup
{
    private static RedisContainer? _redisContainer;
    private static PostgreSqlContainer? _postgreSqlContainer;

    public static string? RedisConnectionString => _redisContainer?.GetConnectionString();
    public static string? DbConnectionString => _postgreSqlContainer?.GetConnectionString();
    public static bool IsDockerAvailable { get; private set; }

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        try
        {
            _redisContainer = new RedisBuilder()
                .WithImage("redis:alpine")
                .Build();

            _postgreSqlContainer = new PostgreSqlBuilder()
                .WithImage("postgres:15-alpine")
                .WithDatabase("marketours_test")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await Task.WhenAll(
                _redisContainer.StartAsync(),
                _postgreSqlContainer.StartAsync()
            );

            var options = new DbContextOptionsBuilder<MarketContext>()
                .UseNpgsql(DbConnectionString)
                .Options;

            using var context = new MarketContext(options);
            await context.Database.EnsureCreatedAsync();
            
            IsDockerAvailable = true;
        }
        catch (Exception ex)
        {
            IsDockerAvailable = false;
            Console.WriteLine("##### DOCKER NOT AVAILABLE - INTEGRATION TESTS WILL BE SKIPPED #####");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    [OneTimeTearDown]
    public async Task GlobalTearDown()
    {
        if (_redisContainer != null) await _redisContainer.DisposeAsync();
        if (_postgreSqlContainer != null) await _postgreSqlContainer.DisposeAsync();
    }
}