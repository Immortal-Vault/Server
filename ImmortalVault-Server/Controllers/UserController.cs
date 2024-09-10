using ImmortalVault_Server.Models;
using ImmortalVault_Server.Services.Auth;
using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImmortalVault_Server.Controllers;

public record ChangeLanguageModel(string Email, string Language);

[ApiController]
[Route("api/user")]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public UserController(ApplicationDbContext dbContext)
    {
        this._dbContext = dbContext;
    }
    
    [HttpPost("changeLanguage")]
    public async Task<IActionResult> ChangeLanguage([FromBody] ChangeLanguageModel model)
    {
        var user = await this._dbContext.Users
            .Include(u => u.UserLocalization)
            .Where(u => u.Email == model.Email)
            .FirstOrDefaultAsync();
        
        if (user is null)
        {
            return StatusCode(404);
        }

        try
        {
            if (user.UserLocalization is { })
            {
                user.UserLocalization.Language = model.Language;
                this._dbContext.UsersLocalizations.Update(user.UserLocalization);
            }
            else
            {
                var userLocalization = new UserLocalization()
                {
                    Language = model.Language,
                    UserId = user.Id,
                };

                user.UserLocalization = userLocalization;
                this._dbContext.UsersLocalizations.Add(userLocalization);
            }
            
            this._dbContext.Users.Update(user);

            await this._dbContext.SaveChangesAsync();

            return Ok();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return StatusCode(500, "An error occurred while updating the user's language.");
        }
    }
}