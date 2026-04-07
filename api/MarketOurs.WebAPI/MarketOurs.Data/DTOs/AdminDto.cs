using System.ComponentModel.DataAnnotations;

namespace MarketOurs.Data.DTOs;

public class AdminOverviewDto
{
    public int TotalUsers { get; set; }

    public int ActiveUsers { get; set; }

    public int TotalPosts { get; set; }

    public int PostsCreatedInLast7Days { get; set; }

    public int TotalLogs { get; set; }

    public int ErrorLogs { get; set; }

    public int BlacklistHits { get; set; }

    public int CacheHits { get; set; }

    public List<AdminTrendPointDto> PostTrend { get; set; } = [];

    public List<AdminRecentActivityDto> RecentActivities { get; set; } = [];

    public AdminSystemSummaryDto SystemSummary { get; set; } = new();
}

public class AdminTrendPointDto
{
    public DateTime Date { get; set; }

    public int Posts { get; set; }

    public int Users { get; set; }
}

public class AdminRecentActivityDto
{
    public string Id { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }
}

public class AdminSystemSummaryDto
{
    public string SiteName { get; set; } = "MarketOurs";

    public bool AllowRegistration { get; set; } = true;

    public bool MaintenanceMode { get; set; }

    public int MaxPostImages { get; set; } = 9;

    public bool AutoApprovePosts { get; set; } = true;
}

public class AdminSettingsDto
{
    [Required]
    [MaxLength(128)]
    public string SiteName { get; set; } = "MarketOurs";

    public bool AllowRegistration { get; set; } = true;

    public bool MaintenanceMode { get; set; }

    [Range(1, 20)]
    public int MaxPostImages { get; set; } = 9;

    public bool AutoApprovePosts { get; set; } = true;

    [EmailAddress]
    [MaxLength(512)]
    public string SupportEmail { get; set; } = string.Empty;

    [MaxLength(1024)]
    public string Announcement { get; set; } = string.Empty;
}

public class UpdateUserStatusRequest
{
    public bool IsActive { get; set; }
}
