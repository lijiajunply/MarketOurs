using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using MarketOurs.DataAPI.Configs;

namespace MarketOurs.DataAPI.Services;

public interface IAIService
{
    Kernel GetKernel();
    Task<string> GetChatResponseAsync(string message);
    // You can add more generic methods here as needed for the project
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
