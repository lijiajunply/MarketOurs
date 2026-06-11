using MarketOurs.Data;
using MarketOurs.DataAPI.Configs;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace MarketOurs.Test.Services;

/// <summary>
/// FollowService 缓存一致性回归测试。
///
/// 背景：历史 bug —— 关系计数走 Redis Set，但 Set 通过增量 SetAdd/SetRemove 维护，
/// 在冷启动 / TTL 过期 / 屏蔽路径下会变成“残缺集合”，导致 SetLength 计数错乱
/// （粉丝数莫名跳变、取关后仍出现在列表中）。
///
/// 修复后的核心不变量（这些测试守护它）：
///   Redis 中的关系集合要么是数据库的“完整镜像”，要么不存在。
///   - key 存在  → 直接用其成员计数；
///   - key 不存在 → 从数据库全量重建整个集合，绝不返回残缺值。
/// </summary>
[TestFixture]
public class FollowServiceTests
{
    private Mock<IConnectionMultiplexer> _mockRedis;
    private Mock<IDatabase> _mockDatabase;
    private Mock<IUserRepo> _mockUserRepo;
    private Mock<ILockService> _mockLockService;
    private IMemoryCache _memoryCache;
    private FollowService _service;

    [SetUp]
    public void Setup()
    {
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockDatabase = new Mock<IDatabase>();
        _mockUserRepo = new Mock<IUserRepo>();
        _mockLockService = new Mock<ILockService>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());

        _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_mockDatabase.Object);
        var redisList = new List<IConnectionMultiplexer> { _mockRedis.Object };

        // DbContextFactory 仅被写路径使用；本测试只覆盖读路径，给一个不会被调用的 mock 即可。
        var mockFactory = new Mock<IDbContextFactory<MarketContext>>();

        _service = new FollowService(
            _mockUserRepo.Object,
            mockFactory.Object,
            redisList,
            _mockLockService.Object,
            _memoryCache,
            new Mock<ILogger<FollowService>>().Object);
    }

    [TearDown]
    public void TearDown() => _memoryCache.Dispose();

    [Test]
    public async Task GetFollowStats_WhenFollowersKeyExists_UsesRedisMembers()
    {
        // Arrange：followers 集合已缓存，含 2 个完整成员
        const string userId = "userB";
        var followersKey = CacheKeys.UserFollowers(userId);
        var followingKey = CacheKeys.UserFollowing(userId);

        _mockDatabase.Setup(db => db.KeyExistsAsync(followersKey, It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        _mockDatabase.Setup(db => db.SetMembersAsync(followersKey, It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue[] { "userA", "userC" });

        _mockDatabase.Setup(db => db.KeyExistsAsync(followingKey, It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        _mockDatabase.Setup(db => db.SetMembersAsync(followingKey, It.IsAny<CommandFlags>()))
            .ReturnsAsync(Array.Empty<RedisValue>());

        // Act
        var stats = await _service.GetFollowStatsAsync(userId);

        // Assert：粉丝数 = Redis 集合成员数
        Assert.That(stats.FollowerCount, Is.EqualTo(2));
        // 未回源数据库
        _mockUserRepo.Verify(r => r.GetFollowerIdsAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task GetFollowStats_WhenFollowersKeyMissing_RebuildsFromDatabase()
    {
        // Arrange：followers 集合缓存缺失（冷启动 / TTL 过期），数据库里其实有 3 个粉丝。
        // 这是历史 bug 的核心场景 —— 旧实现会返回残缺 SetLength；修复后必须全量重建为 3。
        const string userId = "userB";
        var followersKey = CacheKeys.UserFollowers(userId);
        var followingKey = CacheKeys.UserFollowing(userId);

        _mockDatabase.Setup(db => db.KeyExistsAsync(followersKey, It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);
        _mockDatabase.Setup(db => db.KeyExistsAsync(followingKey, It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        _mockUserRepo.Setup(r => r.GetFollowerIdsAsync(userId))
            .ReturnsAsync(["userA", "userC", "userD"]);
        _mockUserRepo.Setup(r => r.GetFollowingIdsAsync(userId))
            .ReturnsAsync([]);

        // Act
        var stats = await _service.GetFollowStatsAsync(userId);

        // Assert：粉丝数 = 数据库全量值
        Assert.That(stats.FollowerCount, Is.EqualTo(3));
        // 并把完整集合写回 Redis（重建），而非逐个增量
        _mockDatabase.Verify(
            db => db.SetAddAsync(
                followersKey,
                It.Is<RedisValue[]>(v => v.Length == 3),
                It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Test]
    public async Task GetFollowStats_WhenViewerProvided_IsFollowingReflectsRebuiltSet()
    {
        // Arrange：viewer 关注了 target。viewer 的 following 集合缓存缺失，需从数据库重建后再判断。
        const string targetId = "userB";
        const string viewerId = "userA";

        // target 自身的统计集合（重建为空即可，本用例不关心计数）
        _mockDatabase.Setup(db => db.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);
        _mockUserRepo.Setup(r => r.GetFollowerIdsAsync(It.IsAny<string>())).ReturnsAsync([]);
        _mockUserRepo.Setup(r => r.GetFollowingIdsAsync(targetId)).ReturnsAsync([]);
        _mockUserRepo.Setup(r => r.GetBlockedUserIdsByMeAsync(It.IsAny<string>())).ReturnsAsync([]);

        // viewer 的 following 集合：数据库中包含 target
        _mockUserRepo.Setup(r => r.GetFollowingIdsAsync(viewerId)).ReturnsAsync([targetId]);

        // Act
        var stats = await _service.GetFollowStatsAsync(targetId, viewerId);

        // Assert：基于重建后的完整集合，正确判定 viewer 正在关注 target
        Assert.That(stats.IsFollowing, Is.True);
    }
}
