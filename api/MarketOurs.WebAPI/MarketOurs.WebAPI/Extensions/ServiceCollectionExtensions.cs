using Amazon.S3;
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
        services.AddScoped<IPostTagRepo, PostTagRepo>();
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
        services.AddHostedService<UploadKeyCleanupService>();

        // Services
        services.AddScoped<UploadKeyService>();
        services.AddScoped<ILockService, RedisLockService>();
        services.AddScoped<ILikeManager, LikeManager>();
        services.AddScoped<IFollowService, FollowService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<IPostTagService, PostTagService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<INotificationService, NotificationService>();
        RegisterPushService(services);
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

    private static void RegisterPushService(IServiceCollection services)
    {
        services.AddHttpClient();

        var serviceAccountPath = Environment.GetEnvironmentVariable("FIREBASE_SERVICE_ACCOUNT_PATH", EnvironmentVariableTarget.Process);
        var projectId = Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID", EnvironmentVariableTarget.Process);
        var jpushAppKey = Environment.GetEnvironmentVariable("JPUSH_APP_KEY", EnvironmentVariableTarget.Process);
        var jpushMasterSecret = Environment.GetEnvironmentVariable("JPUSH_MASTER_SECRET", EnvironmentVariableTarget.Process);
        var jpushNotificationChannelId = Environment.GetEnvironmentVariable("JPUSH_NOTIFICATION_CHANNEL_ID", EnvironmentVariableTarget.Process);

        services.AddSingleton<IPushService, PushService>();

        if (!string.IsNullOrWhiteSpace(jpushAppKey) && !string.IsNullOrWhiteSpace(jpushMasterSecret))
        {
            services.AddSingleton<IPushProvider>(sp =>
                new JPushProvider(
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(JPushProvider)),
                    sp.GetRequiredService<ILogger<JPushProvider>>(),
                    jpushAppKey,
                    jpushMasterSecret,
                    jpushNotificationChannelId));
        }

        if (!string.IsNullOrWhiteSpace(serviceAccountPath) && File.Exists(serviceAccountPath))
        {
            services.AddSingleton<IPushProvider>(sp =>
                new FirebasePushProvider(
                    sp.GetRequiredService<ILogger<FirebasePushProvider>>(),
                    serviceAccountPath,
                    projectId));
        }

        services.AddSingleton<IPushProvider, MockPushProvider>();
    }

    /// <summary>
    /// 注册安全相关服务
    /// </summary>
    public static void RegisterSecurityServices(this IServiceCollection services)
    {
        // 注册存储服务
        services.AddScoped<LocalStorageService>();
        services.AddScoped<ImageProcessingService>();

        var storageProvider =
            Environment.GetEnvironmentVariable("STORAGE_PROVIDER", EnvironmentVariableTarget.Process) ?? "";

        if (storageProvider.Equals("S3", StringComparison.OrdinalIgnoreCase))
        {
            var s3Config = services.BuildServiceProvider().GetRequiredService<S3StorageConfig>();
            var s3ClientConfig = new AmazonS3Config { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(s3Config.Region) };
            if (!string.IsNullOrWhiteSpace(s3Config.Endpoint))
            {
                s3ClientConfig.ServiceURL = s3Config.Endpoint;
                s3ClientConfig.ForcePathStyle = s3Config.ForcePathStyle;
            }

            services.AddSingleton<IAmazonS3>(_ =>
                new AmazonS3Client(s3Config.AccessKey, s3Config.SecretKey, s3ClientConfig));
            services.AddScoped<IStorageService, S3StorageService>();
        }
        else if (storageProvider.Equals("Local", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IStorageService, LocalStorageService>();
        }
        else
        {
            services.AddHttpClient<IStorageService, VercelBlobStorageService>();
        }

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
            WebRefreshTokenExpiryHours =
                int.TryParse(
                    Environment.GetEnvironmentVariable("JWT_WEB_REFRESH_TOKEN_EXPIRY_HOURS",
                        EnvironmentVariableTarget.Process),
                    out var webRefreshTokenExpiry)
                    ? webRefreshTokenExpiry
                    : int.TryParse(
                        Environment.GetEnvironmentVariable("JWT_REFRESH_TOKEN_EXPIRY_HOURS",
                            EnvironmentVariableTarget.Process),
                        out var legacyRefreshTokenExpiry)
                        ? legacyRefreshTokenExpiry
                        : 72,
            MobileRefreshTokenExpiryHours =
                int.TryParse(
                    Environment.GetEnvironmentVariable("JWT_MOBILE_REFRESH_TOKEN_EXPIRY_HOURS",
                        EnvironmentVariableTarget.Process),
                    out var mobileRefreshTokenExpiry)
                    ? mobileRefreshTokenExpiry
                    : 720,
            DesktopRefreshTokenExpiryHours =
                int.TryParse(
                    Environment.GetEnvironmentVariable("JWT_DESKTOP_REFRESH_TOKEN_EXPIRY_HOURS",
                        EnvironmentVariableTarget.Process),
                    out var desktopRefreshTokenExpiry)
                    ? desktopRefreshTokenExpiry
                    : 720,
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
                Environment.GetEnvironmentVariable("AI_CONTENT_SAFETY_API_KEY", EnvironmentVariableTarget.Process),
            ReviewFailOpen = !bool.TryParse(
                Environment.GetEnvironmentVariable("AI_REVIEW_FAIL_OPEN", EnvironmentVariableTarget.Process),
                out var reviewFailOpen) || reviewFailOpen
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

        var vercelBlobConfig = new VercelBlobConfig()
        {
            Token =
                Environment.GetEnvironmentVariable("BLOB_READ_WRITE_TOKEN", EnvironmentVariableTarget.Process) ?? "",
            StoreId = NormalizeStoreId(
                Environment.GetEnvironmentVariable("BLOB_STORE_ID", EnvironmentVariableTarget.Process)
                ?? ParseStoreId(
                    Environment.GetEnvironmentVariable("BLOB_READ_WRITE_TOKEN", EnvironmentVariableTarget.Process) ??
                    "")),
            Access = Environment.GetEnvironmentVariable("BLOB_ACCESS", EnvironmentVariableTarget.Process) ?? "public",
            BaseUrl = (Environment.GetEnvironmentVariable("BLOB_BASE_PATH", EnvironmentVariableTarget.Process) ??
                       "uploads")
                .Trim('/'),
            CacheControlMaxAgeSeconds = int.TryParse(
                Environment.GetEnvironmentVariable("BLOB_CACHE_CONTROL_MAX_AGE", EnvironmentVariableTarget.Process),
                out var cacheSeconds)
                ? cacheSeconds
                : 31536000
        };

        var s3Config = new S3StorageConfig
        {
            AccessKey = Environment.GetEnvironmentVariable("S3_ACCESS_KEY", EnvironmentVariableTarget.Process) ?? "",
            SecretKey = Environment.GetEnvironmentVariable("S3_SECRET_KEY", EnvironmentVariableTarget.Process) ?? "",
            BucketName = Environment.GetEnvironmentVariable("S3_BUCKET", EnvironmentVariableTarget.Process) ?? "",
            Region = Environment.GetEnvironmentVariable("S3_REGION", EnvironmentVariableTarget.Process) ?? "us-east-1",
            Endpoint = Environment.GetEnvironmentVariable("S3_ENDPOINT", EnvironmentVariableTarget.Process),
            BasePrefix = (Environment.GetEnvironmentVariable("S3_BASE_PREFIX", EnvironmentVariableTarget.Process) ?? "uploads").Trim('/'),
            CdnBaseUrl = Environment.GetEnvironmentVariable("S3_CDN_URL", EnvironmentVariableTarget.Process),
            ForcePathStyle = bool.TryParse(
                Environment.GetEnvironmentVariable("S3_FORCE_PATH_STYLE", EnvironmentVariableTarget.Process),
                out var forcePathStyle) && forcePathStyle
        };

        services.AddSingleton(jwtConfig);
        services.AddSingleton(emailConfig);
        services.AddSingleton(aiConfig);
        services.AddSingleton(smsConfig);
        services.AddSingleton(vercelBlobConfig);
        services.AddSingleton(s3Config);
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

    private static string NormalizeStoreId(string storeId)
    {
        return storeId.StartsWith("store_", StringComparison.OrdinalIgnoreCase)
            ? storeId["store_".Length..]
            : storeId;
    }

    private static string ParseStoreId(string token)
    {
        var parts = token.Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 4 && !string.IsNullOrWhiteSpace(parts[3]))
        {
            return NormalizeStoreId(parts[3]);
        }

        throw new InvalidOperationException(
            "无法从 BLOB_READ_WRITE_TOKEN 解析 store id，请显式配置 BLOB_STORE_ID。");
    }
}
