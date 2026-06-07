using MarketOurs.DataAPI.Configs;
using MarketOurs.DataAPI.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketOurs.Test.Services;

[TestFixture]
public class ReviewServiceTests
{
    [Test]
    public async Task Review_WhenSensitiveWordMatched_ShouldRejectWithoutCallingAi()
    {
        var aiService = new Mock<IAIService>();
        var service = CreateService(aiService, ["赌博"]);

        var result = await service.Review("这里有赌博内容");

        Assert.That(result, Is.EqualTo("出现敏感词"));
        aiService.Verify(s => s.Review(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Review_WhenAiUnavailableAndFailOpen_ShouldApprove()
    {
        var aiService = CreateUnavailableAiService();
        var service = CreateService(aiService, [], reviewFailOpen: true);

        var result = await service.Review("普通校园二手书");

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task Review_WhenAiUnavailableAndFailClosed_ShouldReject()
    {
        var aiService = CreateUnavailableAiService();
        var service = CreateService(aiService, [], reviewFailOpen: false);

        var result = await service.Review("普通校园二手书");

        Assert.That(result, Is.EqualTo("AI审核服务暂不可用，请稍后重试"));
    }

    private static ReviewService CreateService(
        Mock<IAIService> aiService,
        IEnumerable<string> sensitiveWords,
        bool reviewFailOpen = true)
    {
        var sensitiveWordService = new Mock<ISensitiveWordService>();
        sensitiveWordService
            .Setup(s => s.GetFilterAsync())
            .ReturnsAsync(new SensitiveWordFilter(sensitiveWords));

        return new ReviewService(
            aiService.Object,
            sensitiveWordService.Object,
            new AIConfig { ReviewFailOpen = reviewFailOpen },
            Mock.Of<ILogger<ReviewService>>());
    }

    private static Mock<IAIService> CreateUnavailableAiService()
    {
        var aiService = new Mock<IAIService>();
        aiService
            .Setup(s => s.Review(It.IsAny<string>()))
            .ThrowsAsync(new AIReviewUnavailableException("AI review service is unavailable.", new InvalidOperationException()));

        return aiService;
    }
}
