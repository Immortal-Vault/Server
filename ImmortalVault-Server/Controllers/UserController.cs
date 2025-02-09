using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImmortalVault_Server.Controllers;

public record ChangeLanguageModel(string Language);

public record ChangeTimeFormatModel(bool Is12HoursFormat);

[ApiController]
[Route("api/user")]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public UserController(ApplicationDbContext dbContext)
    {
        this._dbContext = dbContext;
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
}