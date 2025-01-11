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
        var email = this.User.FindFirst(ClaimTypes.Email)!.Value;

        var user = await this._dbContext.Users
            .Include(u => u.UserSettings)
            .Where(u => u.Email == email)
            .FirstOrDefaultAsync();

        if (user is null)
        {
            return this.NotFound();
        }

        try
        {
            user.UserSettings.Language = model.Language;
            this._dbContext.UsersSettings.Update(user.UserSettings);

            await this._dbContext.SaveChangesAsync();

            return this.Ok();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return this.StatusCode(500, "An error occurred while updating the user's language.");
        }
    }
    
    [Authorize]
    [HttpPost("changeTimeFormat")]
    public async Task<IActionResult> ChangeTimeFormat([FromBody] ChangeTimeFormatModel model)
    {
        var email = this.User.FindFirst(ClaimTypes.Email)!.Value;

        var user = await this._dbContext.Users
            .Include(u => u.UserSettings)
            .Where(u => u.Email == email)
            .FirstOrDefaultAsync();

        if (user is null)
        {
            return this.NotFound();
        }

        try
        {
            user.UserSettings.Is12HoursFormat = model.Is12HoursFormat;
            this._dbContext.UsersSettings.Update(user.UserSettings);

            await this._dbContext.SaveChangesAsync();

            return this.Ok();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return this.StatusCode(500, "An error occurred while updating the user's language.");
        }
    }
}