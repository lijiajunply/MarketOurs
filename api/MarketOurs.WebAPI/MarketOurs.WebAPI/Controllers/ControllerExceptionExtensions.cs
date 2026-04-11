using System.Security.Claims;
using MarketOurs.DataAPI.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace MarketOurs.WebAPI.Controllers;

public static class ControllerExceptionExtensions
{
    public static string GetRequiredUserId(this ControllerBase controller)
    {
        var userId = controller.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new AuthException(ErrorCode.Unauthorized, "未授权");
        }

        return userId;
    }
}
