using MarketOurs.DataAPI.Configs;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services;
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
        services.AddSingleton<DataAPI.Services.Background.LikeMessageQueue>();
        services.AddSingleton<DataAPI.Services.Background.NotificationMessageQueue>();
        services.AddHostedService<DataAPI.Services.Background.LikeSyncBackgroundService>();
        services.AddHostedService<DataAPI.Services.Background.NotificationSyncBackgroundService>();
        services.AddHostedService<DataAPI.Services.Background.DailyHotListBackgroundService>();

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
}