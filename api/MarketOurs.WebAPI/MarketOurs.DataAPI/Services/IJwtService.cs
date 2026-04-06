using System.Security.Claims;
using MarketOurs.Data.DTOs;
using Microsoft.IdentityModel.Tokens;

namespace MarketOurs.DataAPI.Services;

/// <summary>
/// JWT 令牌服务接口，处理 AccessToken 和 RefreshToken 的生成与验证
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// 根据用户信息和设备类型生成短期访问令牌 (AccessToken)
    /// </summary>
    /// <param name="user">用户信息</param>
    /// <param name="deviceType">设备类型枚举</param>
    /// <returns>加密后的 JWT 字符串</returns>
    public Task<string> GetAccessToken(UserDto user, DeviceType deviceType);

    /// <summary>
    /// 生成长期刷新令牌 (RefreshToken)
    /// </summary>
    /// <param name="deviceType">设备类型</param>
    /// <returns>随机生成的令牌字符串</returns>
    public Task<string> GetRefreshToken(DeviceType deviceType);

    /// <summary>
    /// 验证 AccessToken 的合法性并提取声明 (Claims)
    /// </summary>
    /// <param name="token">待验证的令牌</param>
    /// <param name="validationParameters">可选的验证参数</param>
    /// <returns>验证结果及提取出的用户声明列表</returns>
    (bool isValid, IEnumerable<Claim> claims) ValidateAccessToken(string token,
        TokenValidationParameters? validationParameters = null);
}

/// <summary>
/// 登录设备类型枚举
/// </summary>
public enum DeviceType
{
    /// <summary>
    /// 移动端 (iOS/Android)
    /// </summary>
    Mobile,

    /// <summary>
    /// 桌面端
    /// </summary>
    Desktop,

    /// <summary>
    /// Web 浏览器端
    /// </summary>
    Web,

    /// <summary>
    /// 未知设备
    /// </summary>
    Unknown
}

/// <summary>
/// 设备类型扩展方法类
/// </summary>
public static class DeviceTypeExtensions
{
    /// <summary>
    /// 将字符串转换为 DeviceType 枚举
    /// </summary>
    public static DeviceType GetDeviceTypeEnum(this string type)
    {
        return Enum.TryParse(type, out DeviceType result) ? result : DeviceType.Unknown;
    }
}