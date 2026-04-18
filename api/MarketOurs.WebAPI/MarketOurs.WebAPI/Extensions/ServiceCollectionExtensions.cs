using MarketOurs.DataAPI.Configs;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services;
using MarketOurs.DataAPI.Services.Background;
using MarketOurs.WebAPI.Services;
using Microsoft.SemanticKernel;

namespace MarketOurs.WebAPI.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册仓库和服务
    /// </summary>
    public static void RegisterRepositoriesAndServices(this IServiceCollection services)
    {
        // Repositories
        services.AddScoped<IUserRepo, UserRepo>();
        services.AddScoped<IPostRepo, PostRepo>();
        services.AddScoped<ICommentRepo, CommentRepo>();
        services.AddScoped<INotificationRepo, NotificationRepo>();
        services.AddScoped<IAdminRepo, AdminRepo>();
        services.AddScoped<ISensitiveWordRepo, SensitiveWordRepo>();

        // Background queue for async DB sync
        services.AddSingleton<LikeMessageQueue>();
        services.AddSingleton<NotificationMessageQueue>();
        services.AddSingleton<ReviewMessageQueue>();
        services.AddHostedService<LikeSyncBackgroundService>();
        services.AddHostedService<NotificationSyncBackgroundService>();
        services.AddHostedService<ReviewBackgroundService>();
        services.AddHostedService<DailyHotListBackgroundService>();

        // Services
        services.AddScoped<ILockService, RedisLockService>();
        services.AddScoped<ILikeManager, LikeManager>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IPushService, MockPushService>();
        services.AddScoped<ITemplateService, FluidTemplateService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ISmsService, UniSmsService>();
        services.AddScoped<ILoginService, LoginService>();
        services.AddScoped<IAIService, AIService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<ISensitiveWordService, SensitiveWordService>();

        services.AddScoped<IJwtService, JwtService>();
    }

    /// <summary>
    /// 注册安全相关服务
    /// </summary>
    public static void RegisterSecurityServices(this IServiceCollection services)
    {
        // 注册存储服务
        services.AddScoped<IStorageService, LocalStorageService>();

        // 注册IP黑名单缓存服务
        services.AddSingleton<IIpBlacklistCacheService, IpBlacklistCacheService>();

        // 注册速率限制服务及配置
        services.AddSingleton<RateLimitConfig>();
        services.AddSingleton<RateLimitService>();

        // 注册数据脱敏服务及配置
        services.AddSingleton<MaskingConfig>();
        services.AddSingleton<DataMaskingService>();
    }

    public static void ConfigServices(this IServiceCollection services)
    {
        var jwtConfig = new JwtConfig
        {
            AccessTokenExpiryMinutes =
                int.TryParse(
                    Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_EXPIRY_MINUTES",
                        EnvironmentVariableTarget.Process),
                    out var accessTokenExpiry)
                    ? accessTokenExpiry
                    : 20,
            RefreshTokenExpiryHours =
                int.TryParse(
                    Environment.GetEnvironmentVariable("JWT_REFRESH_TOKEN_EXPIRY_HOURS",
                        EnvironmentVariableTarget.Process),
                    out var refreshTokenExpiry)
                    ? refreshTokenExpiry
                    : 72,
            RsaPrivateKeyPath =
                Environment.GetEnvironmentVariable("JWT_RSA_PRIVATE_KEY_PATH", EnvironmentVariableTarget.Process) ??
                "./keys/rsa_private.pem",
            RsaPublicKeyPath = Environment.GetEnvironmentVariable("JWT_RSA_PUBLIC_KEY_PATH") ??
                               "./keys/rsa_public.pem",
            Issuer =
                Environment.GetEnvironmentVariable("JWT_ISSUER", EnvironmentVariableTarget.Process) ?? "MarketOurs",
            Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE", EnvironmentVariableTarget.Process) ??
                       "MarketOurs",
            KeyRotationDays = int.TryParse(
                Environment.GetEnvironmentVariable("JWT_KEY_ROTATION_DAYS", EnvironmentVariableTarget.Process),
                out var keyRotationDays)
                ? keyRotationDays
                : 90
        };

        var emailConfig = new EmailConfig()
        {
            Host = Environment.GetEnvironmentVariable("EMAIL_HOST", EnvironmentVariableTarget.Process) ?? "localhost",
            Port = Convert.ToInt32(
                Environment.GetEnvironmentVariable("EMAIL_PORT", EnvironmentVariableTarget.Process) ?? "564"),
            Username = Environment.GetEnvironmentVariable("EMAIL_USERNAME", EnvironmentVariableTarget.Process),
            Password = Environment.GetEnvironmentVariable("EMAIL_PASSWORD", EnvironmentVariableTarget.Process),
            Email = Environment.GetEnvironmentVariable("EMAIL")
        };

        var aiConfig = new AIConfig
        {
            ApiKey = Environment.GetEnvironmentVariable("AI_API_KEY", EnvironmentVariableTarget.Process),
            ModelId = Environment.GetEnvironmentVariable("AI_MODEL_ID", EnvironmentVariableTarget.Process) ?? "gpt-4o",
            Endpoint = Environment.GetEnvironmentVariable("AI_ENDPOINT", EnvironmentVariableTarget.Process),
            OrgId = Environment.GetEnvironmentVariable("AI_ORG_ID", EnvironmentVariableTarget.Process),
            Provider = Environment.GetEnvironmentVariable("AI_PROVIDER", EnvironmentVariableTarget.Process) ?? "OpenAI",
            DeploymentName =
                Environment.GetEnvironmentVariable("AI_DEPLOYMENT_NAME", EnvironmentVariableTarget.Process),
            ContentSafetyEndpoint =
                Environment.GetEnvironmentVariable("AI_CONTENT_SAFETY_ENDPOINT", EnvironmentVariableTarget.Process),
            ContentSafetyApiKey =
                Environment.GetEnvironmentVariable("AI_CONTENT_SAFETY_API_KEY", EnvironmentVariableTarget.Process)
        };

        var smsConfig = new SmsConfig()
        {
            AccessKeyId = Environment.GetEnvironmentVariable("SMS_ACCESSKEY_ID", EnvironmentVariableTarget.Process) ??
                          "",
            AccessKeySecret =
                Environment.GetEnvironmentVariable("SMS_ACCESSKEY_SECRET", EnvironmentVariableTarget.Process) ?? "",
            Signature = Environment.GetEnvironmentVariable("SMS_SIGNATURE", EnvironmentVariableTarget.Process) ??
                        "MarketOurs",
            Endpoint = Environment.GetEnvironmentVariable("SMS_ENDPOINT", EnvironmentVariableTarget.Process) ??
                       "uni.apistd.com",
            SigningAlgorithm =
                Environment.GetEnvironmentVariable("SMS_SIGNING_ALGORITHM", EnvironmentVariableTarget.Process) ??
                "hmac-sha256",
        };

        services.AddSingleton(jwtConfig);
        services.AddSingleton(emailConfig);
        services.AddSingleton(aiConfig);
        services.AddSingleton(smsConfig);
        services.AddSingleton<RsaKeyManager>();

        var kernelBuilder = services.AddKernel();
        if (aiConfig.Provider?.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase) == true)
        {
            kernelBuilder.AddAzureOpenAIChatCompletion(
                aiConfig.DeploymentName ?? "gpt-4o",
                aiConfig.Endpoint ?? string.Empty,
                aiConfig.ApiKey ?? string.Empty,
                aiConfig.ModelId ?? "gpt-4o");
        }
        else // Default to OpenAI
        {
            kernelBuilder.AddOpenAIChatCompletion(
                modelId: aiConfig.ModelId ?? "deepseek-chat",
                apiKey: aiConfig.ApiKey ?? string.Empty,
                endpoint: new Uri(aiConfig.Endpoint ?? string.Empty));
        }
    }
}
