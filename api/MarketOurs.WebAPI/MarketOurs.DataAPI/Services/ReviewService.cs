namespace MarketOurs.DataAPI.Services;

public interface IReviewService
{
    public Task<string> Review(string textToReview);
}

public class ReviewService(IAIService aiService, ISensitiveWordService sensitiveWordService) : IReviewService
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
        
        return await aiService.Review(textToReview);
    }
}