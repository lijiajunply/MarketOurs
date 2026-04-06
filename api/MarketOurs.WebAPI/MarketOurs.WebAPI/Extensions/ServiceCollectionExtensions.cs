using MarketOurs.DataAPI.Configs;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services;
using MarketOurs.DataAPI.Services.Background;
using MarketOurs.WebAPI.Services;

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

        // Background queue for async DB sync
        services.AddSingleton<LikeMessageQueue>();
        services.AddSingleton<NotificationMessageQueue>();
        services.AddHostedService<LikeSyncBackgroundService>();
        services.AddHostedService<NotificationSyncBackgroundService>();
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
                int.TryParse(Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_EXPIRY_MINUTES"),
                    out var accessTokenExpiry)
                    ? accessTokenExpiry
                    : 20,
            RefreshTokenExpiryHours =
                int.TryParse(Environment.GetEnvironmentVariable("JWT_REFRESH_TOKEN_EXPIRY_HOURS"),
                    out var refreshTokenExpiry)
                    ? refreshTokenExpiry
                    : 72,
            RsaPrivateKeyPath = Environment.GetEnvironmentVariable("JWT_RSA_PRIVATE_KEY_PATH") ??
                                "./app/keys/rsa_private.pem",
            RsaPublicKeyPath = Environment.GetEnvironmentVariable("JWT_RSA_PUBLIC_KEY_PATH") ??
                               "./app/keys/rsa_public.pem",
            Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "MarketOurs",
            Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "MarketOurs",
            KeyRotationDays = int.TryParse(Environment.GetEnvironmentVariable("JWT_KEY_ROTATION_DAYS"),
                out var keyRotationDays)
                ? keyRotationDays
                : 90
        };

        var emailConfig = new EmailConfig()
        {
            Host = Environment.GetEnvironmentVariable("EMAIL_HOST") ?? "localhost",
            Port = Convert.ToInt32(Environment.GetEnvironmentVariable("EMAIL_PORT") ?? "564"),
            Username = Environment.GetEnvironmentVariable("EMAIL_USERNAME"),
            Password = Environment.GetEnvironmentVariable("EMAIL_PASSWORD"),
            Email = Environment.GetEnvironmentVariable("EMAIL")
        };

        var aiConfig = new AIConfig
        {
            ApiKey = Environment.GetEnvironmentVariable("AI_API_KEY"),
            ModelId = Environment.GetEnvironmentVariable("AI_MODEL_ID") ?? "gpt-4o",
            Endpoint = Environment.GetEnvironmentVariable("AI_ENDPOINT"),
            OrgId = Environment.GetEnvironmentVariable("AI_ORG_ID"),
            Provider = Environment.GetEnvironmentVariable("AI_PROVIDER") ?? "OpenAI",
            DeploymentName = Environment.GetEnvironmentVariable("AI_DEPLOYMENT_NAME")
        };

        var smsConfig = new SmsConfig()
        {
            AccessKeyId = Environment.GetEnvironmentVariable("SMS_ACCESSKEY_ID") ?? "",
            AccessKeySecret = Environment.GetEnvironmentVariable("SMS_ACCESSKEY_SECRET") ?? "",
            Endpoint = Environment.GetEnvironmentVariable("SMS_ENDPOINT") ?? "uni.apistd.com",
            SigningAlgorithm = Environment.GetEnvironmentVariable("SMS_SIGNING_ALGORITHM") ?? "hmac-sha256",
        };

        services.AddSingleton(jwtConfig);
        services.AddSingleton(emailConfig);
        services.AddSingleton(aiConfig);
        services.AddSingleton(smsConfig);
        services.AddSingleton<RsaKeyManager>();
    }
}