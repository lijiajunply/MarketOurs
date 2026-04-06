using Microsoft.Extensions.Logging;

namespace MarketOurs.DataAPI.Services;

/// <summary>
/// 推送服务接口，处理移动端或浏览器的通知推送
/// </summary>
public interface IPushService
{
    /// <summary>
    /// 发送推送通知
    /// </summary>
    /// <param name="pushToken">目标设备的推送令牌</param>
    /// <param name="title">通知标题</param>
    /// <param name="body">通知正文</param>
    /// <param name="data">附加数据载荷</param>
    Task SendPushNotificationAsync(string pushToken, string title, string body,
        IDictionary<string, string>? data = null);
}

/// <summary>
/// 模拟推送服务，仅记录日志，用于开发和测试环境
/// </summary>
public class MockPushService(ILogger<MockPushService> logger) : IPushService
{
    /// <inheritdoc/>
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