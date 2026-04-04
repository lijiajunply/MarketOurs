using MarketOurs.DataAPI.Services;
using Moq;
using StackExchange.Redis;

namespace MarketOurs.Test.Concurrency;

[TestFixture]
[Category("Concurrency")]
public class DistributedLockTests
{
    private Mock<IConnectionMultiplexer> _mockRedis;
    private Mock<IDatabase> _mockDatabase;
    private RedisLockService _lockService;

    [SetUp]
    public void Setup()
    {
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockDatabase = new Mock<IDatabase>();
        _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockDatabase.Object);
        
        _lockService = new RedisLockService(new List<IConnectionMultiplexer> { _mockRedis.Object });
    }

    [Test]
    public async Task AcquireAsync_WhenRedisReturnsTrue_ShouldReturnTrue()
    {
        // Arrange
        _mockDatabase.Setup(db => db.LockTakeAsync("test-key", "test-val", It.IsAny<TimeSpan>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var result = await _lockService.AcquireAsync("test-key", "test-val", TimeSpan.FromSeconds(1));

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task AcquireAsync_ConcurrentAttempts_OnlyOneSucceeds()
    {
        // Arrange
        int callCount = 0;
        _mockDatabase.Setup(db => db.LockTakeAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(() => 
            {
                // Simulate Redis atomicity: only first call returns true
                return Interlocked.Increment(ref callCount) == 1;
            });

        // Act
        var tasks = new List<Task<bool>>();
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(_lockService.AcquireAsync("lock-key", Guid.NewGuid().ToString(), TimeSpan.FromSeconds(5)));
        }
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.That(results.Count(r => r == true), Is.EqualTo(1));
        Assert.That(results.Count(r => r == false), Is.EqualTo(99));
    }

    [Test]
    public async Task ReleaseAsync_ShouldCallRedisLockRelease()
    {
        // Arrange
        _mockDatabase.Setup(db => db.LockReleaseAsync("test-key", "test-val", It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var result = await _lockService.ReleaseAsync("test-key", "test-val");

        // Assert
        Assert.That(result, Is.True);
        _mockDatabase.Verify(db => db.LockReleaseAsync("test-key", "test-val", It.IsAny<CommandFlags>()), Times.Once);
    }
}