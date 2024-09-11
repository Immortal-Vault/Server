using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ImmortalVault_Server.Models;
using ImmortalVault_Server.Services.Auth;

namespace ImmortalVault_Server.Middlewares;

public class JwtRefreshMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAuthService _authService;

    public JwtRefreshMiddleware(RequestDelegate next, IAuthService authService)
    {
        this._next = next;
        this._authService = authService;
    }

    public async Task Invoke(HttpContext context)
    {
        var token = context.Request.Cookies["jwtToken"];
        if (!string.IsNullOrEmpty(token))
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var exp = jwtToken.ValidTo;
            
            if (exp < DateTime.UtcNow.AddMinutes(AuthService.TokenRefreshThresholdInMinutes))
            {
                var email = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                if (email != null)
                {
                    var newToken = _authService.GenerateAccessToken(email, Audience.ImmortalVaultClient);
                    
                    context.Response.Cookies.Append("jwtToken", newToken, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTimeOffset.UtcNow.AddMinutes(AuthService.TokenLifetimeMinutes)
                    });
                }
            }
        }

        await this._next(context);
    }
}
