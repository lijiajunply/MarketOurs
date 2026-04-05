using MarketOurs.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace MarketOurs.Test.Integration;

[SetUpFixture]
public class TestAssemblySetup
{
    private static RedisContainer? _redisContainer;
    private static PostgreSqlContainer? _postgreSqlContainer;

    public static string? RedisConnectionString => IsDockerAvailable ? _redisContainer?.GetConnectionString() : null;
    public static string? DbConnectionString => IsDockerAvailable ? _postgreSqlContainer?.GetConnectionString() : null;
    public static bool IsDockerAvailable { get; private set; }

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        // On macOS, the Docker Desktop socket path might not be automatically picked up by Testcontainers
        // if DOCKER_HOST is not set. We try to provide a hint if a standard socket exists.
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX) &&
            string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOCKER_HOST")))
        {
            var socketPath = "/var/run/docker.sock";
            if (System.IO.File.Exists(socketPath))
            {
                Environment.SetEnvironmentVariable("DOCKER_HOST", $"unix://{socketPath}");
                TestContext.Progress.WriteLine($"##### AUTO-DETECTED DOCKER SOCKET AT {socketPath} #####");
            }
        }

        try
        {
            TestContext.Progress.WriteLine("##### STARTING DOCKER CONTAINERS #####");

            _redisContainer = new RedisBuilder()
                .WithImage("redis:alpine")
                .Build();

            _postgreSqlContainer = new PostgreSqlBuilder()
                .WithImage("paradedb/paradedb:latest")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            // Start containers sequentially to ensure stability
            await _redisContainer.StartAsync();
            TestContext.Progress.WriteLine("##### REDIS STARTED #####");
            
            await _postgreSqlContainer.StartAsync();
            TestContext.Progress.WriteLine("##### POSTGRES STARTED #####");

            // Use container directly because IsDockerAvailable is still false at this point
            var dbConn = _postgreSqlContainer.GetConnectionString();
            if (string.IsNullOrEmpty(dbConn))
            {
                throw new Exception("PostgreSQL connection string is empty after startup.");
            }

            var options = new DbContextOptionsBuilder<MarketContext>()
                .UseNpgsql(dbConn)
                .Options;

            await using var context = new MarketContext(options);
            
            // Add a small retry logic for database availability
            int retries = 10;
            while (retries > 0)
            {
                try 
                {
                    // 使用 MigrateAsync 以执行包括 ParadeDB 初始化在内的所有迁移
                    await context.Database.MigrateAsync();
                    TestContext.Progress.WriteLine("##### DATABASE MIGRATED SUCCESSFULLY #####");
                    break;
                }
                catch (Exception ex) when (retries > 1)
                {
                    retries--;
                    TestContext.Progress.WriteLine($"##### WAITING FOR DB READY ({retries} RETRIES LEFT) - {ex.Message} #####");
                    await Task.Delay(2000);
                }
            }

            IsDockerAvailable = true;
            TestContext.Progress.WriteLine("##### DOCKER INITIALIZED SUCCESSFULLY #####");
        }
        catch (Exception ex)
        {
            IsDockerAvailable = false;
            TestContext.Progress.WriteLine($"##### DOCKER NOT AVAILABLE - Error: {ex.Message} #####");
            TestContext.Progress.WriteLine($"Stack: {ex.StackTrace}");
        }
    }

    [OneTimeTearDown]
    public async Task GlobalTearDown()
    {
        if (_redisContainer != null) await _redisContainer.DisposeAsync();
        if (_postgreSqlContainer != null) await _postgreSqlContainer.DisposeAsync();
    }
}
