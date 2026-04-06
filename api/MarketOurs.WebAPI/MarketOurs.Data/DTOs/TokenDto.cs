namespace MarketOurs.Data.DTOs;

/// <summary>
/// 令牌对数据传输对象 (双 Token 机制)
/// </summary>
public class TokenDto
{
    /// <summary>
    /// 短期访问令牌 (用于 API 授权)
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// 长期刷新令牌 (用于获取新的 AccessToken)
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}