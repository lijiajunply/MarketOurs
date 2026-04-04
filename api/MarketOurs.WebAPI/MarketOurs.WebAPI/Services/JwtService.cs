using System.Security.Claims;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Services;

namespace MarketOurs.WebAPI.Services;

public class JwtService : IJwtService
{
    public Task<string> GetAccessToken(UserDto user)
    {
        throw new NotImplementedException();
    }

    public Task<string> GetRefreshToken(string token, string deviceType)
    {
        throw new NotImplementedException();
    }

    public (bool isValid, IEnumerable<Claim> claims) ValidateAccessToken(string token)
    {
        throw new NotImplementedException();
    }
}