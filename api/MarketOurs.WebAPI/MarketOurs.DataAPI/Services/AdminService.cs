using MarketOurs.Data.DataModels;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Repos;

namespace MarketOurs.DataAPI.Services;

public interface IAdminService
{
    Task<AdminOverviewDto> GetOverviewAsync();
    Task<AdminSettingsDto> GetSettingsAsync();
    Task<AdminSettingsDto> UpdateSettingsAsync(AdminSettingsDto request);
    Task<UserDto?> UpdateUserStatusAsync(string id, bool isActive);
}

public class AdminService(IAdminRepo adminRepo) : IAdminService
{
    public async Task<AdminOverviewDto> GetOverviewAsync()
    {
        var now = DateTime.UtcNow;
        var sevenDaysAgo = now.AddDays(-6).Date;

        var totalUsersTask = adminRepo.GetTotalUsersAsync();
        var activeUsersTask = adminRepo.GetActiveUsersAsync();
        var totalPostsTask = adminRepo.GetTotalPostsAsync();
        var postsCreatedTask = adminRepo.GetPostsCreatedSinceAsync(sevenDaysAgo);
        var settingsTask = adminRepo.GetOrCreateSystemSettingsAsync();
        var postCountsTask = adminRepo.GetDailyPostCountsAsync(sevenDaysAgo, now.Date);
        var userCountsTask = adminRepo.GetDailyUserCountsAsync(sevenDaysAgo, now.Date);
        var recentActivitiesTask = adminRepo.GetRecentActivitiesAsync();
        var logStatsTask = adminRepo.GetLogStatsAsync();
        var hitStatsTask = adminRepo.GetBlacklistAndCacheStatsAsync();

        await Task.WhenAll(
            totalUsersTask,
            activeUsersTask,
            totalPostsTask,
            postsCreatedTask,
            settingsTask,
            postCountsTask,
            userCountsTask,
            recentActivitiesTask,
            logStatsTask,
            hitStatsTask);

        var (totalLogs, errorLogs) = await logStatsTask;
        var (blacklistHits, cacheHits) = await hitStatsTask;
        var settings = await settingsTask;

        return new AdminOverviewDto
        {
            TotalUsers = await totalUsersTask,
            ActiveUsers = await activeUsersTask,
            TotalPosts = await totalPostsTask,
            PostsCreatedInLast7Days = await postsCreatedTask,
            TotalLogs = totalLogs,
            ErrorLogs = errorLogs,
            BlacklistHits = blacklistHits,
            CacheHits = cacheHits,
            PostTrend = BuildTrend(sevenDaysAgo, now.Date, await postCountsTask, await userCountsTask),
            RecentActivities = MapRecentActivities(await recentActivitiesTask),
            SystemSummary = MapToSummaryDto(settings)
        };
    }

    public async Task<AdminSettingsDto> GetSettingsAsync()
    {
        var settings = await adminRepo.GetOrCreateSystemSettingsAsync();
        return MapToSettingsDto(settings);
    }

    public async Task<AdminSettingsDto> UpdateSettingsAsync(AdminSettingsDto request)
    {
        var settings = await adminRepo.GetOrCreateSystemSettingsAsync();

        settings.SiteName = request.SiteName.Trim();
        settings.AllowRegistration = request.AllowRegistration;
        settings.MaintenanceMode = request.MaintenanceMode;
        settings.MaxPostImages = request.MaxPostImages;
        settings.AutoApprovePosts = request.AutoApprovePosts;
        settings.SupportEmail = request.SupportEmail.Trim();
        settings.Announcement = request.Announcement.Trim();
        settings.UpdatedAt = DateTime.UtcNow;

        await adminRepo.UpdateSystemSettingsAsync(settings);

        return MapToSettingsDto(settings);
    }

    public async Task<UserDto?> UpdateUserStatusAsync(string id, bool isActive)
    {
        var user = await adminRepo.GetUserByIdAsync(id);
        if (user == null)
        {
            return null;
        }

        user.IsActive = isActive;
        await adminRepo.UpdateUserAsync(user);
        return MapToUserDto(user);
    }

    private static List<AdminTrendPointDto> BuildTrend(
        DateTime startDate,
        DateTime endDate,
        IReadOnlyDictionary<DateTime, int> postCounts,
        IReadOnlyDictionary<DateTime, int> userCounts)
    {
        return Enumerable.Range(0, (endDate - startDate).Days + 1)
            .Select(offset => startDate.AddDays(offset))
            .Select(date => new AdminTrendPointDto
            {
                Date = date,
                Posts = postCounts.GetValueOrDefault(date, 0),
                Users = userCounts.GetValueOrDefault(date, 0)
            })
            .ToList();
    }

    private static List<AdminRecentActivityDto> MapRecentActivities(
        IEnumerable<(string Id, string Type, string Title, string Description, DateTime Timestamp)> activities)
    {
        return activities.Select(activity => new AdminRecentActivityDto
        {
            Id = activity.Id,
            Type = activity.Type,
            Title = activity.Title,
            Description = activity.Description,
            Timestamp = activity.Timestamp
        }).ToList();
    }

    private static AdminSettingsDto MapToSettingsDto(SystemSettingsModel settings)
    {
        return new AdminSettingsDto
        {
            SiteName = settings.SiteName,
            AllowRegistration = settings.AllowRegistration,
            MaintenanceMode = settings.MaintenanceMode,
            MaxPostImages = settings.MaxPostImages,
            AutoApprovePosts = settings.AutoApprovePosts,
            SupportEmail = settings.SupportEmail,
            Announcement = settings.Announcement
        };
    }

    private static AdminSystemSummaryDto MapToSummaryDto(SystemSettingsModel settings)
    {
        return new AdminSystemSummaryDto
        {
            SiteName = settings.SiteName,
            AllowRegistration = settings.AllowRegistration,
            MaintenanceMode = settings.MaintenanceMode,
            MaxPostImages = settings.MaxPostImages,
            AutoApprovePosts = settings.AutoApprovePosts
        };
    }

    private static UserDto MapToUserDto(UserModel user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Phone = user.Phone,
            Name = user.Name,
            Role = user.Role,
            Avatar = user.Avatar,
            Info = user.Info,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            IsActive = user.IsActive,
            IsEmailVerified = user.IsEmailVerified,
            IsPhoneVerified = user.IsPhoneVerified,
            GithubId = user.GithubId,
            GoogleId = user.GoogleId,
            WeixinId = user.WeixinId,
            OursId = user.OursId,
            PushSettings = user.PushSettings
        };
    }
}
