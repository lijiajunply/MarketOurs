using Microsoft.Extensions.Logging;

namespace MarketOurs.DataAPI.Services;

public interface IPushService
{
    Task SendPushNotificationAsync(string pushToken, string title, string body,
        IDictionary<string, string>? data = null);
}

public class MockPushService(ILogger<MockPushService> logger) : IPushService
{
    public Task SendPushNotificationAsync(string pushToken, string title, string body,
        IDictionary<string, string>? data = null)
    {
        logger.LogInformation("[PUSH MOCK] Sending push to {Token}: {Title} - {Body}", pushToken, title, body);
        if (data != null)
        {
            foreach (var kv in data)
            {
                logger.LogInformation("[PUSH MOCK] Data: {Key}={Value}", kv.Key, kv.Value);
            }
        }

        return Task.CompletedTask;
    }
}