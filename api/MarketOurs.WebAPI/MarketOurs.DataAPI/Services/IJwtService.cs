using System.Security.Claims;
using MarketOurs.Data.DTOs;

namespace MarketOurs.DataAPI.Services;

public interface IJwtService
{
    public Task<string> GetAccessToken(UserDto user);
    public Task<string> GetRefreshToken(string token, string deviceType);
    (bool isValid, IEnumerable<Claim> claims) ValidateAccessToken(string token);
}

public enum DeviceType
{
    WeChat,
    Mobile,
    Desktop,
    Web,
    Unknown
}

public static class DeviceTypeExtensions
{
    public static string GetString(this DeviceType deviceType)
    {
        return deviceType.ToString();
    }

    public static DeviceType? GetDeviceTypeEnum(this string type)
    {
        if (Enum.TryParse(type, out DeviceType result))
        {
            return result;
        }

        return null;
    }
}