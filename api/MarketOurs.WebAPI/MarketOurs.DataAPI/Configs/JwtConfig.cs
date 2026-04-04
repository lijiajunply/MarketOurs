using System.ComponentModel.DataAnnotations;

namespace MarketOurs.DataAPI.Configs;

/// <summary>
/// JWT配置类，用于管理JWT相关配置项
/// </summary>
[Serializable]
public class JwtConfig
{
    /// <summary>
    /// 访问令牌过期时间（分钟），建议15-30分钟
    /// </summary>
    [Range(15, 30)]
    public int AccessTokenExpiryMinutes { get; set; } = 20;

    /// <summary>
    /// 刷新令牌过期时间（小时）
    /// </summary>
    [Range(24, 168)]
    public int RefreshTokenExpiryHours { get; set; } = 72;

    /// <summary>
    /// RSA私钥路径
    /// </summary>
    [Required]
    public string RsaPrivateKeyPath { get; set; } = "";

    /// <summary>
    /// RSA公钥路径
    /// </summary>
    [Required]
    public string RsaPublicKeyPath { get; set; } = "";

    /// <summary>
    /// 签发者
    /// </summary>
    [Required]
    public string Issuer { get; set; } = "iOS Club of XAUAT";

    /// <summary>
    /// 接收者
    /// </summary>
    [Required]
    public string Audience { get; set; } = "iOS Club of XAUAT";

    /// <summary>
    /// 密钥轮换周期（天）
    /// </summary>
    [Range(30, 365)]
    public int KeyRotationDays { get; set; } = 90;
}