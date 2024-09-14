using ImmortalVault_Server.Models;
using ImmortalVault_Server.Services.Auth;
using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImmortalVault_Server.Controllers;

public record SignUpModel(string Username, string Email, string Password);
public record SignInModel(string Email, string Password);

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ApplicationDbContext _dbContext;

    public AuthController(IAuthService authService, ApplicationDbContext dbContext)
    {
        this._authService = authService;
        this._dbContext = dbContext;
    }
    
    [HttpPost("signUp")]
    public async Task<IActionResult> SignUp([FromBody] SignUpModel model)
    {
        var sameUser = await this._dbContext.Users
            .Where(u => u.Email == model.Email || u.Name == model.Username)
            .FirstOrDefaultAsync();
        if (sameUser != null)
        {
            return StatusCode(303);
        }
        
        try
        {
            var user = new User
            {
                Name = model.Username,
                Email = model.Email,
                Password = Argon2.Hash(model.Password)
            };
            
            this._dbContext.Users.Add(user);
            await this._dbContext.SaveChangesAsync();

            return Ok();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return StatusCode(500, "An error occurred while creating the user.");
        }
    }
    
    [HttpPost("signIn")]
    public async Task<IActionResult> SignIn([FromBody] SignInModel model)
    {
        var user = await _dbContext.Users
            .Include(user => user.UserLocalization)
            .FirstOrDefaultAsync(u => u.Email == model.Email);
        if (user is null)
        {
            return NotFound();
        }
        
        if (!Argon2.Verify(user.Password, model.Password))
        {
            return StatusCode(409);
        }

        var token = this._authService.GenerateAccessToken(user.Email, Audience.ImmortalVaultClient);
        Response.Cookies.Append("immortalVaultJwtToken", token, new CookieOptions()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddMinutes(AuthService.TokenLifetimeMinutes)
        });
        
        var localization = user.UserLocalization?.Language;

        return Ok(new { localization });
    }
    
    [Authorize]
    [HttpPost("signOut")]
    public IActionResult SignOut()
    {
        Response.Cookies.Delete("immortalVaultJwtToken");
        return Ok();
    }
    
    [Authorize]
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok();
    }
}