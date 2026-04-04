using System.Security.Claims;
using MarketOurs.Data.DTOs;
using Microsoft.IdentityModel.Tokens;

namespace MarketOurs.DataAPI.Services;

public interface IJwtService
{
    public Task<string> GetAccessToken(UserDto user, DeviceType deviceType);
    public Task<string> GetRefreshToken(DeviceType deviceType);

    (bool isValid, IEnumerable<Claim> claims) ValidateAccessToken(string token,
        TokenValidationParameters? validationParameters = null);
}

public enum DeviceType
{
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

    public static DeviceType GetDeviceTypeEnum(this string type)
    {
        return Enum.TryParse(type, out DeviceType result) ? result : DeviceType.Unknown;
    }
}