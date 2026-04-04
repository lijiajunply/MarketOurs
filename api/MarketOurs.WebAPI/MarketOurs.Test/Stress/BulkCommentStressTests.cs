using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MarketOurs.Data.DataModels;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketOurs.Test.Stress;

[TestFixture]
[Category("HighLoad")]
public class BulkCommentStressTests
{
    private Mock<ICommentRepo> _mockCommentRepo;
    private Mock<IUserRepo> _mockUserRepo;
    private Mock<ILikeManager> _mockLikeManager;
    private Mock<IMemoryCache> _mockMemoryCache;
    private Mock<IDistributedCache> _mockDistributedCache;
    private Mock<ILogger<CommentService>> _mockLogger;
    private CommentService _commentService;

    [SetUp]
    public void Setup()
    {
        _mockCommentRepo = new Mock<ICommentRepo>();
        _mockUserRepo = new Mock<IUserRepo>();
        _mockLikeManager = new Mock<ILikeManager>();
        _mockMemoryCache = new Mock<IMemoryCache>();
        _mockDistributedCache = new Mock<IDistributedCache>();
        _mockLogger = new Mock<ILogger<CommentService>>();

        // Basic MemoryCache mock setups
        _mockMemoryCache.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(new Mock<ICacheEntry>().Object);

        _commentService = new CommentService(
            _mockCommentRepo.Object,
            _mockUserRepo.Object,
            _mockLikeManager.Object,
            _mockMemoryCache.Object,
            _mockDistributedCache.Object,
            _mockLogger.Object
        );
    }

    [Test]
    public async Task CreateAsync_BulkConcurrentWrites_ShouldHandle10000Comments()
    {
        // Arrange
        const int totalComments = 10000;
        var user = new UserModel { Id = "user_1" };
        _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(user);
        
        int dbInsertCount = 0;
        _mockCommentRepo.Setup(r => r.CreateAsync(It.IsAny<CommentModel>()))
            .Returns(Task.CompletedTask)
            .Callback(() => Interlocked.Increment(ref dbInsertCount));

        var createDtos = Enumerable.Range(0, totalComments).Select(i => new CommentCreateDto
        {
            UserId = "user_1",
            PostId = "post_1",
            Content = $"Comment {i}"
        }).ToList();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        // Use a higher degree of parallelism to stress the service layer
        await Parallel.ForEachAsync(createDtos, new ParallelOptions { MaxDegreeOfParallelism = 100 }, async (dto, _) =>
        {
            await _commentService.CreateAsync(dto);
        });
        stopwatch.Stop();

        // Assert
        TestContext.Out.WriteLine($"Bulk Comment Creation: {totalComments} comments in {stopwatch.ElapsedMilliseconds}ms");
        TestContext.Out.WriteLine($"Throughput: {totalComments / stopwatch.Elapsed.TotalSeconds:F2} comments/s");
        
        Assert.That(dbInsertCount, Is.EqualTo(totalComments), "Not all comments were sent to the repository!");
        _mockMemoryCache.Verify(m => m.Remove(It.IsAny<object>()), Times.AtLeast(totalComments));
    }
}