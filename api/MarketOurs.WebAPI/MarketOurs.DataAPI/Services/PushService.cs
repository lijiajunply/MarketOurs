using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;

namespace MarketOurs.DataAPI.Services;

/// <summary>
/// 推送 Provider 类型
/// </summary>
public static class PushProviderType
{
    public const string JPush = "jpush";
    public const string Firebase = "firebase";
    public const string Mock = "mock";
}

/// <summary>
/// 统一推送请求模型
/// </summary>
public sealed class PushSendRequest
{
    public string Provider { get; init; } = string.Empty;
    public string PushToken { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public IDictionary<string, string>? Data { get; init; }
}

/// <summary>
/// 统一推送异常
/// </summary>
public sealed class PushSendException(
    string message,
    bool isTokenInvalid,
    Exception? innerException = null) : Exception(message, innerException)
{
    public bool IsTokenInvalid { get; } = isTokenInvalid;
}

/// <summary>
/// 业务侧统一推送入口
/// </summary>
public interface IPushService
{
    Task SendPushNotificationAsync(PushSendRequest request);
}

/// <summary>
/// 可插拔推送 Provider 接口
/// </summary>
public interface IPushProvider
{
    string ProviderId { get; }

    Task SendAsync(PushSendRequest request, CancellationToken cancellationToken = default);
}

public sealed class PushService(IEnumerable<IPushProvider> providers) : IPushService
{
    private readonly IReadOnlyDictionary<string, IPushProvider> _providers = providers
        .ToDictionary(x => x.ProviderId, StringComparer.OrdinalIgnoreCase);

    public Task SendPushNotificationAsync(PushSendRequest request)
    {
        if (!_providers.TryGetValue(request.Provider, out var provider))
        {
            if (_providers.TryGetValue(PushProviderType.Mock, out var mockProvider))
            {
                return mockProvider.SendAsync(request);
            }

            throw new PushSendException($"Push provider '{request.Provider}' is not registered", false);
        }

        return provider.SendAsync(request);
    }
}

/// <summary>
/// Firebase Cloud Messaging Provider
/// </summary>
public sealed class FirebasePushProvider : IPushProvider
{
    private const string DefaultChannelId = "marketours_notifications";
    private static readonly object SyncRoot = new();
    private static FirebaseApp? _firebaseApp;

    private readonly FirebaseMessaging _messaging;
    private readonly ILogger<FirebasePushProvider> _logger;

    public FirebasePushProvider(
        ILogger<FirebasePushProvider> logger,
        string serviceAccountPath,
        string? projectId = null)
    {
        _logger = logger;
        _messaging = FirebaseMessaging.GetMessaging(GetOrCreateApp(serviceAccountPath, projectId));
    }

    public string ProviderId => PushProviderType.Firebase;

    public async Task SendAsync(PushSendRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new Message
            {
                Token = request.PushToken,
                Notification = new Notification
                {
                    Title = request.Title,
                    Body = request.Body
                },
                Data = request.Data?.ToDictionary(kv => kv.Key, kv => kv.Value ?? string.Empty) ??
                       new Dictionary<string, string>(),
                Android = new AndroidConfig
                {
                    Priority = Priority.High,
                    Notification = new AndroidNotification
                    {
                        ChannelId = DefaultChannelId
                    }
                }
            };

            await _messaging.SendAsync(message, cancellationToken);
        }
        catch (FirebaseMessagingException ex) when (IsInvalidTokenError(ex))
        {
            _logger.LogWarning(ex, "Firebase push token is invalid and should be cleared");
            throw new PushSendException("Push token is invalid", true, ex);
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogError(ex, "Firebase push send failed");
            throw new PushSendException("Push send failed", false, ex);
        }
    }

    private static FirebaseApp GetOrCreateApp(string serviceAccountPath, string? projectId)
    {
        lock (SyncRoot)
        {
            if (_firebaseApp != null)
            {
                return _firebaseApp;
            }

            var options = new AppOptions
            {
                Credential = GoogleCredential.FromFile(serviceAccountPath)
            };

            if (!string.IsNullOrWhiteSpace(projectId))
            {
                options.ProjectId = projectId;
            }

            _firebaseApp = FirebaseApp.Create(options, "MarketOursFirebasePush");
            return _firebaseApp;
        }
    }

    private static bool IsInvalidTokenError(FirebaseMessagingException ex)
    {
        return ex.MessagingErrorCode is MessagingErrorCode.InvalidArgument or MessagingErrorCode.Unregistered;
    }
}

/// <summary>
/// 极光推送 Provider
/// </summary>
public sealed class JPushProvider : IPushProvider
{
    private const string ApiUrl = "https://api.jpush.cn/v3/push";
    private const string DefaultNotificationChannelId = "marketours_notifications";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<JPushProvider> _logger;
    private readonly string _notificationChannelId;

    public JPushProvider(
        HttpClient httpClient,
        ILogger<JPushProvider> logger,
        string appKey,
        string masterSecret,
        string? notificationChannelId = null)
    {
        _httpClient = httpClient;
        _logger = logger;
        _notificationChannelId = string.IsNullOrWhiteSpace(notificationChannelId)
            ? DefaultNotificationChannelId
            : notificationChannelId.Trim();

        var credentialBytes = Encoding.UTF8.GetBytes($"{appKey}:{masterSecret}");
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentialBytes));
    }

    public string ProviderId => PushProviderType.JPush;

    public async Task SendAsync(PushSendRequest request, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            platform = "android",
            audience = new
            {
                registration_id = new[] { request.PushToken }
            },
            notification = new
            {
                android = new
                {
                    alert = request.Body,
                    title = request.Title,
                    builder_id = 1,
                    channel_id = _notificationChannelId,
                    extras = request.Data ?? new Dictionary<string, string>()
                }
            },
            options = new
            {
                apns_production = false
            }
        };

        using var response = await _httpClient.PostAsJsonAsync(ApiUrl, payload, JsonOptions, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var isTokenInvalid = IsInvalidTokenResponse((int)response.StatusCode, body);

        if (isTokenInvalid)
        {
            _logger.LogWarning("JPush registration id is invalid. Response: {Response}", body);
            throw new PushSendException("Push token is invalid", true);
        }

        _logger.LogError("JPush push send failed. StatusCode: {StatusCode}, Response: {Response}",
            (int)response.StatusCode, body);
        throw new PushSendException("Push send failed", false);
    }

    private static bool IsInvalidTokenResponse(int statusCode, string body)
    {
        if (statusCode == 400 && body.Contains("registration_id", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return body.Contains("invalid registration_id", StringComparison.OrdinalIgnoreCase) ||
               body.Contains("registration id", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// 模拟推送 Provider，仅记录日志，用于开发和测试环境
/// </summary>
public sealed class MockPushProvider(ILogger<MockPushProvider> logger) : IPushProvider
{
    public string ProviderId => PushProviderType.Mock;

    public Task SendAsync(PushSendRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[PUSH MOCK] Provider={Provider} Token={Token}: {Title} - {Body}",
            request.Provider, request.PushToken, request.Title, request.Body);

        if (request.Data != null)
        {
            foreach (var kv in request.Data)
            {
                logger.LogInformation("[PUSH MOCK] Data: {Key}={Value}", kv.Key, kv.Value);
            }
        }

        return Task.CompletedTask;
    }
}
