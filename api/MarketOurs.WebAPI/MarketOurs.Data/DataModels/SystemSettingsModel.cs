using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketOurs.Data.DataModels;

[Table("system_settings")]
public class SystemSettingsModel : DataModel
{
    [Key]
    [Column("id")]
    [MaxLength(64)]
    public string Id { get; set; } = "default";

    [MaxLength(128)]
    public string SiteName { get; set; } = "MarketOurs";

    public bool AllowRegistration { get; set; } = true;

    public bool MaintenanceMode { get; set; }

    public int MaxPostImages { get; set; } = 9;

    public bool AutoApprovePosts { get; set; } = true;

    [MaxLength(512)]
    public string SupportEmail { get; set; } = string.Empty;

    [MaxLength(1024)]
    public string Announcement { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public override void Update(DataModel model)
    {
        if (model is not SystemSettingsModel settingsModel)
        {
            return;
        }

        SiteName = settingsModel.SiteName;
        AllowRegistration = settingsModel.AllowRegistration;
        MaintenanceMode = settingsModel.MaintenanceMode;
        MaxPostImages = settingsModel.MaxPostImages;
        AutoApprovePosts = settingsModel.AutoApprovePosts;
        SupportEmail = settingsModel.SupportEmail;
        Announcement = settingsModel.Announcement;
        UpdatedAt = settingsModel.UpdatedAt;
    }
}
