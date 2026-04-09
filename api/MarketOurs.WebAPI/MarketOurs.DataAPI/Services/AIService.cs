using Azure.AI.ContentSafety;
using MarketOurs.Data.DTOs;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using MarketOurs.DataAPI.Configs;

namespace MarketOurs.DataAPI.Services;

/// <summary>
/// AI 服务接口，封装了基于 Semantic Kernel 的大模型交互能力
/// </summary>
public interface IAIService
{
    /// <summary>
    /// 获取底层的 Semantic Kernel 实例，用于更复杂的插件或链式调用
    /// </summary>
    Kernel GetKernel();

    /// <summary>
    /// 获取 AI 的简单对话响应
    /// </summary>
    /// <param name="message">用户输入的消息</param>
    /// <returns>AI 生成的文本内容</returns>
    Task<string> GetChatResponseAsync(string message);

    /// <summary>
    /// 内容审查
    /// </summary>
    /// <param name="postDto"></param>
    /// <returns></returns>
    Task<string> Review(PostDto postDto);
}

public class AIService(
    Kernel kernel,
    IChatCompletionService chatCompletionService,
    ISensitiveWordService sensitiveWordService,
    IServiceProvider serviceProvider)
    : IAIService
{
    /// <inheritdoc/>
    public Kernel GetKernel()
    {
        return kernel;
    }

    public async Task<string> GetChatResponseAsync(string message)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(message);

        var result = await chatCompletionService.GetChatMessageContentAsync(
            chatHistory,
            kernel: kernel);

        return result.Content ?? string.Empty;
    }

    public async Task<string> Review(PostDto postDto)
    {
        var filter = await sensitiveWordService.GetFilterAsync();
        
        if (serviceProvider.GetService(typeof(ContentSafetyClient)) is not ContentSafetyClient contentSafetyClient)
        {
            return "";
        }

        var textToReview = $"{postDto.Title}\n{postDto.Content}";

        if (string.IsNullOrWhiteSpace(textToReview))
        {
            return "";
        }
        
        if (filter.HasSensitiveWord(textToReview))
        {
            return "出现敏感词";
        }

        var request = new AnalyzeTextOptions(textToReview);

        try
        {
            Azure.Response<AnalyzeTextResult> response = await contentSafetyClient.AnalyzeTextAsync(request);

            return response.Value.CategoriesAnalysis != null &&
                   response.Value.CategoriesAnalysis.Any(category => category.Severity > 0)
                ? "出现敏感词"
                : "";
        }
        catch (Azure.RequestFailedException)
        {
            return "出现敏感词";
        }
    }
}
