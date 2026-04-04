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
            services.AddScoped<DataAPI.Repos.IUserRepo, DataAPI.Repos.UserRepo>();
            services.AddScoped<DataAPI.Repos.IPostRepo, DataAPI.Repos.PostRepo>();
            services.AddScoped<DataAPI.Repos.ICommentRepo, DataAPI.Repos.CommentRepo>();

            // Background queue for async DB sync
            services.AddSingleton<DataAPI.Services.Background.LikeMessageQueue>();
            services.AddHostedService<DataAPI.Services.Background.LikeSyncBackgroundService>();

            // Services
            services.AddScoped<DataAPI.Services.ILikeManager, DataAPI.Services.LikeManager>();
            services.AddScoped<DataAPI.Services.IUserService, DataAPI.Services.UserService>();
            services.AddScoped<DataAPI.Services.IPostService, DataAPI.Services.PostService>();
            services.AddScoped<DataAPI.Services.ICommentService, DataAPI.Services.CommentService>();
        }

        /// <summary>
        /// 注册安全相关服务
        /// </summary>
        public void RegisterSecurityServices()
        {
            services.AddSingleton<LogAuditService>();
            
            services.AddSingleton<RsaKeyManager>();

            // 注册IP黑名单缓存服务
            services.AddSingleton<IIpBlacklistCacheService, IpBlacklistCacheService>();
        }
    }
}