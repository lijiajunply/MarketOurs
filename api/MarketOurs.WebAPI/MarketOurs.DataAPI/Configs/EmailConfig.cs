namespace MarketOurs.DataAPI.Configs;


/// <summary>
/// 管理邮件相关配置
/// </summary>
[Serializable]
public class EmailConfig
{
     public string? Host { get; set; }
     public int Port { get; set; }
     public string? Username { get; set; }
     public string? Password { get; set; }
     public string? Email { get; set; }
}