using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

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
    /// <param name="message">内容</param>
    /// <returns></returns>
    Task<string> Review(string message);
}

public class AIService(
    Kernel kernel,
    IChatCompletionService chatCompletionService)
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

    public async Task<string> Review(string message)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage("你是一个专业的内容审查助手。请审查用户输入的内容是否包含违规、敏感、色情、暴力、辱骂等不良信息。" +
                                     "如果内容合规，请仅回复“Pass”。如果不合规，请回复违规的具体原因，不要包含任何多余的解释性文字。");
        chatHistory.AddUserMessage(message);

        var result = await chatCompletionService.GetChatMessageContentAsync(
            chatHistory,
            kernel: kernel);

        var content = result.Content?.Trim() ?? string.Empty;
        if (content.Equals("Pass", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return content;
    }
}