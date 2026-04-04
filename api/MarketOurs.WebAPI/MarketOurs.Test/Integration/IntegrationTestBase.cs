using MarketOurs.Data;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace MarketOurs.Test.Integration;

public abstract class IntegrationTestBase
{
    [SetUp]
    public void CheckDocker()
    {
        if (!TestAssemblySetup.IsDockerAvailable)
        {
            Assert.Ignore("Docker is not available. Integration tests are skipped.");
        }
    }

    protected MarketContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MarketContext>()
            .UseNpgsql(TestAssemblySetup.DbConnectionString)
            .Options;
        return new MarketContext(options);
    }

    protected IConnectionMultiplexer CreateRedisConnection()
    {
        return ConnectionMultiplexer.Connect(TestAssemblySetup.RedisConnectionString!);
    }
}