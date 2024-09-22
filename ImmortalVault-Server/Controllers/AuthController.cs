using System.Security.Claims;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using ImmortalVault_Server.Models;
using ImmortalVault_Server.Services.AES;
using ImmortalVault_Server.Services.Auth;
using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImmortalVault_Server.Controllers;

public record SignUpModel(string Username, string Email, string Password);

public record SignInModel(string Email, string Password);

public record GoogleAuthRequest(string Code);

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _dbContext;

    public AuthController(IAuthService authService, IConfiguration configuration, ApplicationDbContext dbContext)
    {
        this._authService = authService;
        this._configuration = configuration;
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
            .Include(user => user.UserTokens)
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
    [HttpPost("signIn/google")]
    public async Task<IActionResult> SignInGoogle([FromBody] GoogleAuthRequest request)
    {
        var user = await _dbContext.Users
            .Include(user => user.UserTokens)
            .FirstOrDefaultAsync(u => u.Email == User.FindFirst(ClaimTypes.Email)!.Value);
        if (user is null)
        {
            return NotFound();
        }

        try
        {
            var oAuth2Client = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = this._configuration["GOOGLE:CLIENT_ID"],
                    ClientSecret = this._configuration["GOOGLE:CLIENT_SECRET"],
                }
            });

            var tokenResponse = await oAuth2Client.ExchangeCodeForTokenAsync(
                userId: user.Id.ToString(),
                code: request.Code,
                redirectUri: "postmessage",
                CancellationToken.None
            );

            var aesSecretKey = this._configuration["AES:SECRET_KEY"]!;
            var aesIv = this._configuration["AES:IV"]!;

            var encryptedAccessToken = AesEncryption.Encrypt(tokenResponse.AccessToken, aesSecretKey, aesIv);
            var encryptedRefreshToken = AesEncryption.Encrypt(tokenResponse.RefreshToken, aesSecretKey, aesIv);

            if (user.UserTokens is { })
            {
                user.UserTokens.AccessToken = encryptedAccessToken;
                user.UserTokens.RefreshToken = encryptedRefreshToken;

                this._dbContext.UsersTokens.Update(user.UserTokens);
            }
            else
            {
                var userTokens = new UserTokens()
                {
                    AccessToken = encryptedAccessToken,
                    RefreshToken = encryptedRefreshToken,
                };

                user.UserTokens = userTokens;
                this._dbContext.UsersTokens.Add(userTokens);
            }

            this._dbContext.Users.Update(user);

            await this._dbContext.SaveChangesAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("signOut/google")]
    public async Task<IActionResult> SignOutGoogle()
    {
        var user = await _dbContext.Users
            .Include(user => user.UserTokens)
            .FirstOrDefaultAsync(u => u.Email == User.FindFirst(ClaimTypes.Email)!.Value);
        if (user is null)
        {
            return NotFound();
        }

        try
        {
            if (user.UserTokens is null)
            {
                return Ok();
            }
            
            this._dbContext.UsersTokens.Remove(user.UserTokens);
            this._dbContext.Users.Update(user);

            await this._dbContext.SaveChangesAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    
    [Authorize]
    [HttpGet("google")]
    public async Task<IActionResult> GetGoogleState()
    {
        var user = await _dbContext.Users
            .Include(user => user.UserTokens)
            .FirstOrDefaultAsync(u => u.Email == User.FindFirst(ClaimTypes.Email)!.Value);
        if (user is null)
        {
            return NotFound();
        }

        return user.UserTokens is not null ? Ok() : NotFound();
    }

    [Authorize]
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok();
    }
}