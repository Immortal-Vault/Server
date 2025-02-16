using System.Security.Claims;
using ImmortalVault_Server.Models;
using ImmortalVault_Server.Services.Auth;
using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImmortalVault_Server.Controllers;

public record ChangeLanguageModel(string Language);

public record ChangeTimeFormatModel(bool Is12HoursFormat);

public record ChangeInactiveModel(int InactiveMinutes);

public record ChangePasswordModel(string OldPassword, string NewPassword, string? TotpCode);

[ApiController]
[Route("api/user")]
public class UserController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ApplicationDbContext _dbContext;
    private readonly IMfaService _mfaService;

    public UserController(ApplicationDbContext dbContext, IAuthService authService, IMfaService mfaService)
    {
        this._dbContext = dbContext;
        this._authService = authService;
        this._mfaService = mfaService;
    }

    [Authorize]
    [HttpPost("changeLanguage")]
    public async Task<IActionResult> ChangeLanguage([FromBody] ChangeLanguageModel model)
    {
        var email = User.FindFirst(ClaimTypes.Email)!.Value;

        var user = await this._dbContext.Users.AsNoTrackingWithIdentityResolution()
            .Include(u => u.UserSettings)
            .Where(u => u.Email == email)
            .FirstOrDefaultAsync();

        if (user is null)
        {
            return NotFound();
        }

        try
        {
            await this._dbContext.UsersSettings.AsNoTrackingWithIdentityResolution()
                .Where(u => u.Id == user.UserSettings.Id)
                .ExecuteUpdateAsync(us => us
                    .SetProperty(u => u.Language, model.Language)
                );

            return Ok();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return StatusCode(500, "An error occurred while updating the user's language.");
        }
    }

    [Authorize]
    [HttpPost("changeTimeFormat")]
    public async Task<IActionResult> ChangeTimeFormat([FromBody] ChangeTimeFormatModel model)
    {
        var email = User.FindFirst(ClaimTypes.Email)!.Value;

        var user = await this._dbContext.Users.AsNoTrackingWithIdentityResolution()
            .Include(u => u.UserSettings)
            .Where(u => u.Email == email)
            .FirstOrDefaultAsync();

        if (user is null)
        {
            return NotFound();
        }

        try
        {
            await this._dbContext.UsersSettings.AsNoTrackingWithIdentityResolution()
                .Where(us => us.Id == user.UserSettings.Id)
                .ExecuteUpdateAsync(us => us
                    .SetProperty(u => u.Is12HoursFormat, model.Is12HoursFormat)
                );

            return Ok();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return StatusCode(500, "An error occurred while updating the user's time format.");
        }
    }

    [Authorize]
    [HttpPost("changeInactiveMinutes")]
    public async Task<IActionResult> ChangeTimeFormat([FromBody] ChangeInactiveModel model)
    {
        if (model.InactiveMinutes is < 2 or > 9999)
        {
            return BadRequest("INACTIVE_MINUTES_INCORRECT");
        }

        var email = User.FindFirst(ClaimTypes.Email)!.Value;

        var user = await this._dbContext.Users.AsNoTrackingWithIdentityResolution()
            .Include(u => u.UserSettings)
            .Where(u => u.Email == email)
            .FirstOrDefaultAsync();

        if (user is null)
        {
            return NotFound();
        }

        try
        {
            var token = this._authService.GenerateAccessToken(user.Email, Audience.ImmortalVaultClient,
                model.InactiveMinutes);
            Response.Cookies.Append("immortalVaultJwtToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddMinutes(model.InactiveMinutes)
            });

            await this._dbContext.UsersSettings.AsNoTrackingWithIdentityResolution()
                .Where(us => us.Id == user.UserSettings.Id)
                .ExecuteUpdateAsync(us => us
                    .SetProperty(u => u.InactiveMinutes, model.InactiveMinutes)
                );


            return Ok();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return StatusCode(500, "An error occurred while updating the user's inactive minutes.");
        }
    }

    [Authorize]
    [HttpPost("changePassword")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
    {
        var email = User.FindFirst(ClaimTypes.Email)!.Value;

        var user = await this._dbContext.Users.AsNoTrackingWithIdentityResolution()
            .Include(u => u.UserSettings)
            .Where(u => u.Email == email)
            .FirstOrDefaultAsync();

        if (user is null)
        {
            return NotFound();
        }

        if (user.MfaEnabled)
        {
            if (string.IsNullOrEmpty(model.TotpCode))
            {
                return BadRequest("MFA");
            }

            if (!await this._mfaService.UseUserMfa(user, model.TotpCode))
            {
                return BadRequest("INVALID_TOTP");
            }
        }

        if (!Argon2.Verify(user.Password, model.OldPassword))
        {
            return BadRequest("INVALID_PASSWORD");
        }

        var result = await this._authService.ChangePassword(user, model.NewPassword);

        if (!result)
        {
            return BadRequest("SAME_PASSWORD");
        }

        return Ok(result);
    }
}