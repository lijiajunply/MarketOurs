using MarketOurs.DataAPI.Configs;
using Microsoft.Extensions.Logging;

namespace MarketOurs.DataAPI.Services;

public interface IReviewService
{
    public Task<string> Review(string textToReview);
}

public class ReviewService(
    IAIService aiService,
    ISensitiveWordService sensitiveWordService,
    AIConfig aiConfig,
    ILogger<ReviewService> logger) : IReviewService
{
    public async Task<string> Review(string textToReview)
    {
        var filter = await sensitiveWordService.GetFilterAsync();

        if (string.IsNullOrWhiteSpace(textToReview))
        {
            return "";
        }
        
        if (filter.HasSensitiveWord(textToReview))
        {
            return "出现敏感词";
        }

        try
        {
            return await aiService.Review(textToReview);
        }
        catch (AIReviewUnavailableException ex) when (aiConfig.ReviewFailOpen)
        {
            logger.LogWarning(ex, "AI review is unavailable; approving content by fail-open policy.");
            return string.Empty;
        }
        catch (AIReviewUnavailableException ex)
        {
            logger.LogWarning(ex, "AI review is unavailable; rejecting content by fail-closed policy.");
            return "AI审核服务暂不可用，请稍后重试";
        }
    }
}
