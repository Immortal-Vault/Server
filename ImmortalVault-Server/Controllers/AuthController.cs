using System.IdentityModel.Tokens.Jwt;
using ImmortalVault_Server.Models;
using ImmortalVault_Server.Services.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ImmortalVault_Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        this._authService = authService;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginModel model)
    {
        if (model is { Username: "demo", Password: "password" })
        {
            var token = this._authService.GenerateAccessToken(model.Username, "IMMORTAL_VAULT_CLIENT");
            return Ok(new { AccessToken = new JwtSecurityTokenHandler().WriteToken(token) });
        }

        return Unauthorized("Invalid credentials");
    }
}