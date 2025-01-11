using System.Security.Claims;
using ImmortalVault_Server.Models;
using ImmortalVault_Server.Services.Auth;
using Microsoft.EntityFrameworkCore;

namespace ImmortalVault_Server.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/mfa")]
[Authorize]
public class MfaController : ControllerBase
{
    private readonly IMfaService _mfaService;
    private readonly ApplicationDbContext _dbContext;

    public MfaController(IMfaService mfaService, ApplicationDbContext dbContext)
    {
        _mfaService = mfaService;
        _dbContext = dbContext;
    }

    [HttpPost("setup")]
    public async Task<IActionResult> SetupMfa()
    {
        var user = await GetUser();
        if (user is null)
        {
            return Forbid();
        }

        var mfa = _mfaService.SetupMfa(user);
        if (mfa is null)
        {
            return BadRequest("MFA already set up.");
        }

        return Ok(new { mfa });
    }

    [HttpPost("enable")]
    public async Task<IActionResult> EnableMfa([FromBody] EnableMfaRequest request)
    {
        var user = await GetUser();
        if (user is null)
        {
            return Forbid();
        }


        var codes = await _mfaService.EnableMfa(user, request.TotpCode);
        if (codes is null)
        {
            return BadRequest("Invalid TOTP code.");
        }

        return Ok(new { recoveryCodes = codes });
    }

    [HttpPost("disable")]
    public async Task<IActionResult> DisableMfa([FromBody] DisableMfaRequest request)
    {
        var user = await GetUser();
        if (user is null)
        {
            return Forbid();
        }


        var result = await _mfaService.DisableMfa(user, request.Password, request.TotpCode);
        if (!result)
        {
            return BadRequest("Invalid credentials or TOTP code.");
        }

        return Ok("MFA disabled successfully.");
    }

    [HttpPost("validate")]
    public async Task<IActionResult> ValidateMfa([FromBody] ValidateMfaRequest request)
    {
        var user = await GetUser();
        if (user is null)
        {
            return Forbid();
        }

        var isValid = await _mfaService.UseUserMfa(user, request.TotpCode);
        if (!isValid)
        {
            return BadRequest("Invalid TOTP code.");
        }

        return Ok("MFA validated successfully.");
    }

    private Task<User?> GetUser()
    {
        return _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == User.FindFirst(ClaimTypes.Email)!.Value);
    }

    public record EnableMfaRequest(string TotpCode);

    public record DisableMfaRequest(string Password, string TotpCode);

    public record ValidateMfaRequest(string TotpCode);
}