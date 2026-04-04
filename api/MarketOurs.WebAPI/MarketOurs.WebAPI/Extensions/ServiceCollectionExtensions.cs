using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services;
using MarketOurs.WebAPI.Filters;
using MarketOurs.WebAPI.Services;

namespace MarketOurs.WebAPI.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// 注册仓库和服务
        /// </summary>
        public void RegisterRepositoriesAndServices()
        {
            // Repositories
            services.AddScoped<IUserRepo, UserRepo>();
            services.AddScoped<IPostRepo, PostRepo>();
            services.AddScoped<ICommentRepo, CommentRepo>();

            // Background queue for async DB sync
            services.AddSingleton<DataAPI.Services.Background.LikeMessageQueue>();
            services.AddHostedService<DataAPI.Services.Background.LikeSyncBackgroundService>();

            // Services
            services.AddScoped<ILikeManager, LikeManager>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IPostService, PostService>();
            services.AddScoped<ICommentService, CommentService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ILoginService, LoginService>();
            
            services.AddScoped<IJwtService, JwtService>();
        }

        /// <summary>
        /// 注册安全相关服务
        /// </summary>
        public void RegisterSecurityServices()
        {
            services.AddSingleton<LogAuditService>();

            // 注册存储服务
            services.AddScoped<IStorageService, LocalStorageService>();

            // 注册IP黑名单缓存服务
            services.AddSingleton<IIpBlacklistCacheService, IpBlacklistCacheService>();
            services.AddScoped<RateLimitService>();
        }
    }
}