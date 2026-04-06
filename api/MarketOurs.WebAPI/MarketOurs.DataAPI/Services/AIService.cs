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
}

public class AIService : IAIService
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletionService;

    public AIService(AIConfig aiConfig)
    {
        var builder = Kernel.CreateBuilder();

        if (aiConfig.Provider?.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase) == true)
        {
            builder.AddAzureOpenAIChatCompletion(
                aiConfig.DeploymentName ?? "gpt-4o",
                aiConfig.Endpoint ?? string.Empty,
                aiConfig.ApiKey ?? string.Empty,
                aiConfig.ModelId ?? "gpt-4o");
        }
        else // Default to OpenAI
        {
            builder.AddOpenAIChatCompletion(
                aiConfig.ModelId ?? "gpt-4o",
                aiConfig.ApiKey ?? string.Empty,
                aiConfig.OrgId);
        }

        _kernel = builder.Build();
        _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
    }

    /// <inheritdoc/>
    public Kernel GetKernel()
    {
        return _kernel;
    }

    public async Task<string> GetChatResponseAsync(string message)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(message);

        var result = await _chatCompletionService.GetChatMessageContentAsync(
            chatHistory,
            kernel: _kernel);

        return result.Content ?? string.Empty;
    }
}
