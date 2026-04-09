namespace MarketOurs.DataAPI.Configs;

[Serializable]
public class AIConfig
{
    public string? ApiKey { get; set; }
    public string? ModelId { get; set; }
    public string? Endpoint { get; set; }
    public string? OrgId { get; set; }
    public string? Provider { get; set; } // "OpenAI" or "AzureOpenAI"
    public string? DeploymentName { get; set; } // For Azure OpenAI
    public string? ContentSafetyEndpoint { get; set; } // For Azure AI Content Safety
    public string? ContentSafetyApiKey { get; set; } // For Azure AI Content Safety
}
