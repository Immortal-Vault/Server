﻿using System.Security.Claims;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Oauth2.v2;
using Google.Apis.Services;
using ImmortalVault_Server.Models;
using ImmortalVault_Server.Services.AES;
using ImmortalVault_Server.Services.Auth;
using ImmortalVault_Server.Services.GoogleDrive;
using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImmortalVault_Server.Controllers;

public record SignUpModel(string Username, string Email, string Password, string Language, bool Is12HoursFormat);

public record SignInModel(string Email, string Password, string? MfaCode);

public record GoogleAuthRequest(string Code);

public record GoogleSignOutRequest(bool KeepData);

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;
    private readonly IGoogleDriveService _googleDriveService;
    private readonly IMfaService _mfaService;
    private readonly ApplicationDbContext _dbContext;

    private readonly string _aesSecretKey;
    private readonly string _aesIv;

    public AuthController(IAuthService authService, IConfiguration configuration,
        IGoogleDriveService googleDriveService, ApplicationDbContext dbContext, IMfaService mfaService)
    {
        this._authService = authService;
        this._configuration = configuration;
        this._googleDriveService = googleDriveService;
        this._dbContext = dbContext;
        this._mfaService = mfaService;

        this._aesSecretKey = configuration["AES:SECRET_KEY"]!;
        this._aesIv = configuration["AES:IV"]!;
    }

    [HttpPost("signUp")]
    public async Task<IActionResult> SignUp([FromBody] SignUpModel model)
    {
        var sameUser = await this._dbContext.Users.AsNoTrackingWithIdentityResolution()
            .Where(u => u.Email.ToLower() == model.Email.ToLower() ||
                        u.Name.ToLower() == model.Username.ToLower())
            .FirstOrDefaultAsync();
        if (sameUser != null)
        {
            return StatusCode(303);
        }

        try
        {
            var user = new User
            {
                Name = model.Username.ToLower(),
                Email = model.Email.ToLower(),
                Password = Argon2.Hash(model.Password)
            };

            await this._dbContext.Users.AddAsync(user);
            await this._dbContext.SaveChangesAsync();

            var settings = new UserSettings
            {
                UserId = user.Id,
                Language = model.Language,
                Is12HoursFormat = model.Is12HoursFormat
            };

            await this._dbContext.UsersSettings.AddAsync(settings);

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
        var user = await this._dbContext.Users.AsNoTrackingWithIdentityResolution().Include(user => user.UserSettings)
            .Include(user => user.UserTokens)
            .FirstOrDefaultAsync(u =>
                u.Email.ToLower() == model.Email.ToLower() ||
                u.Name.ToLower() == model.Email.ToLower());
        if (user is null)
        {
            return NotFound();
        }

        if (!Argon2.Verify(user.Password, model.Password))
        {
            return StatusCode(409);
        }

        if (user.MfaEnabled)
        {
            if (string.IsNullOrEmpty(model.MfaCode))
            {
                return BadRequest("MFA");
            }

            if (!await this._mfaService.UseUserMfa(user, model.MfaCode))
            {
                return BadRequest("INVALID_MFA");
            }
        }

        var token = this._authService.GenerateAccessToken(user.Email, Audience.ImmortalVaultClient,
            user.UserSettings.InactiveMinutes);
        Response.Cookies.Append("immortalVaultJwtToken", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddMinutes(user.UserSettings.InactiveMinutes)
        });

        var localization = user.UserSettings.Language;
        var is12Hours = user.UserSettings.Is12HoursFormat;
        var username = user.Name;
        var inactiveMinutes = user.UserSettings.InactiveMinutes;

        return Ok(new { localization, is12Hours, username, inactiveMinutes });
    }

    [Authorize]
    [HttpPost("signOut")]
    public new IActionResult SignOut()
    {
        Response.Cookies.Delete("immortalVaultJwtToken");
        return Ok();
    }

    [Authorize]
    [HttpPost("signIn/google")]
    public async Task<IActionResult> SignInGoogle([FromBody] GoogleAuthRequest request)
    {
        var userEmail = User.FindFirst(ClaimTypes.Email)!.Value;
        var user = await this._dbContext.Users.Include(user => user.UserTokens)
            .FirstOrDefaultAsync(u => u.Email == userEmail);

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

            var encryptedAccessToken =
                AesEncryption.Encrypt(tokenResponse.AccessToken, this._aesSecretKey, this._aesIv);
            var encryptedRefreshToken =
                AesEncryption.Encrypt(tokenResponse.RefreshToken, this._aesSecretKey, this._aesIv);
            var tokenExpiryTime = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresInSeconds.GetValueOrDefault(3600));

            if (user.UserTokens is { })
            {
                await this._dbContext.UsersTokens
                    .Where(t => t.Id == user.UserTokens.Id)
                    .ExecuteUpdateAsync(t => t
                        .SetProperty(t => t.AccessToken, encryptedAccessToken)
                        .SetProperty(t => t.RefreshToken, encryptedRefreshToken)
                        .SetProperty(t => t.TokenExpiryTime, tokenExpiryTime)
                    );
                await this._dbContext.Entry(user).ReloadAsync();
            }
            else
            {
                var userTokens = new UserTokens
                {
                    AccessToken = encryptedAccessToken,
                    RefreshToken = encryptedRefreshToken,
                    TokenExpiryTime = tokenExpiryTime,
                    UserId = user.Id
                };

                await this._dbContext.AddAsync(userTokens);
                await this._dbContext.SaveChangesAsync();
            }


            var hasSecretFile = await this._googleDriveService.GetSecretFile(user) is not null;

            var credential = GoogleCredential.FromAccessToken(tokenResponse.AccessToken);
            var userInfoService = new Oauth2Service(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential
            });

            var userInfo = await userInfoService.Userinfo.Get().ExecuteAsync();
            var email = userInfo.Email;

            return Ok(new { hasSecretFile, email });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("signOut/google")]
    public async Task<IActionResult> SignOutGoogle([FromBody] GoogleSignOutRequest request)
    {
        var user = await this._dbContext.Users.AsTracking()
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

            if (!request.KeepData)
            {
                if (this._googleDriveService.IsTokenExpired(user))
                {
                    await this._googleDriveService.UpdateTokens(user);
                }

                await this._googleDriveService.DeleteSecretFile(user);
            }

            await this._dbContext.SaveChangesAsync();
            await this._dbContext.UsersTokens.Where(u => u.Id == user.UserTokens.Id).ExecuteDeleteAsync();
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
        var user = await this._dbContext.Users
            .Include(user => user.UserTokens)
            .FirstOrDefaultAsync(u => u.Email == User.FindFirst(ClaimTypes.Email)!.Value);
        if (user?.UserTokens is null)
        {
            return NotFound();
        }

        if (this._googleDriveService.IsTokenExpired(user))
        {
            await this._googleDriveService.UpdateTokens(user);
        }

        var decryptedAccessToken =
            AesEncryption.Decrypt(user.UserTokens.AccessToken, this._aesSecretKey, this._aesIv);
        var credential = GoogleCredential.FromAccessToken(decryptedAccessToken);

        var userInfoService = new Oauth2Service(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential
        });

        var userInfo = await userInfoService.Userinfo.Get().ExecuteAsync();
        var email = userInfo.Email;

        return Ok(new { email });
    }

    [Authorize]
    [HttpGet("ping")]
    public async Task<IActionResult> Ping()
    {
        var user = await this._dbContext.Users.AsNoTrackingWithIdentityResolution()
            .Include(user => user.UserSettings)
            .FirstOrDefaultAsync(u => u.Email == User.FindFirst(ClaimTypes.Email)!.Value);

        return Ok(user);
    }
}