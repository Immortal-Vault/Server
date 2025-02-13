using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ImmortalVault_Server.Models;
using ImmortalVault_Server.Services.Auth;
using Microsoft.EntityFrameworkCore;

namespace ImmortalVault_Server.Middlewares;

public class JwtRefreshMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuthService _authService;

    public JwtRefreshMiddleware(RequestDelegate next, IAuthService authService, ApplicationDbContext dbContext)
    {
        this._next = next;
        this._authService = authService;
        this._dbContext = dbContext;
    }

    public async Task Invoke(HttpContext context)
    {
        var token = context.Request.Cookies["immortalVaultJwtToken"];
        if (!string.IsNullOrEmpty(token))
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var exp = jwtToken.ValidTo;
            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ??
                        context.User.FindFirst(ClaimTypes.Email)!.Value;

            var inactiveMinutes = (await this._dbContext.Users.AsNoTrackingWithIdentityResolution()
                .Include(u => u.UserSettings)
                .Where(u => u.Email == email)
                .FirstOrDefaultAsync())?.UserSettings.InactiveMinutes ?? 10;
            var part = inactiveMinutes / 2;
            if (exp < DateTime.UtcNow.AddMinutes(part))
            {
                var newToken =
                    this._authService.GenerateAccessToken(email, Audience.ImmortalVaultClient, inactiveMinutes);
                context.Response.Cookies.Append("immortalVaultJwtToken", newToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(inactiveMinutes)
                });
            }
        }

        await this._next(context);
    }
}