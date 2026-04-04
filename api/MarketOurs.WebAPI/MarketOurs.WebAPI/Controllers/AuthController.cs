using MarketOurs.DataAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketOurs.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController(LoginService loginService, UserService userService, ILogger<AuthController> logger)
    : ControllerBase
{
}