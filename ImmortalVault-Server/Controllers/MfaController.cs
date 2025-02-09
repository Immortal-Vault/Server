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
    public record MfaRequest(string TotpCode);

    private readonly IMfaService _mfaService;
    private readonly ApplicationDbContext _dbContext;

    public MfaController(IMfaService mfaService, ApplicationDbContext dbContext)
    {
        this._mfaService = mfaService;
        this._dbContext = dbContext;
    }

    [HttpPost("setup")]
    public async Task<IActionResult> SetupMfa()
    {
        var user = await this.GetUser();
        if (user is null)
        {
            return Forbid();
        }

        var mfa = this._mfaService.SetupMfa(user);
        if (mfa is null)
        {
            return BadRequest("MFA_ALREADY_EXISTS");
        }

        return Ok(new { mfa });
    }

    [HttpPost("enable")]
    public async Task<IActionResult> EnableMfa([FromBody] MfaRequest request)
    {
        var user = await this.GetUser();
        if (user is null)
        {
            return Forbid();
        }


        var codes = await this._mfaService.EnableMfa(user, request.TotpCode);
        if (codes is null)
        {
            return BadRequest("INVALID_TOTP");
        }

        return Ok(new { recoveryCodes = codes });
    }

    [HttpPost("disable")]
    public async Task<IActionResult> DisableMfa([FromBody] MfaRequest request)
    {
        var user = await this.GetUser();
        if (user is null)
        {
            return Forbid();
        }


        var result = await this._mfaService.DisableMfa(user, request.TotpCode);
        if (!result)
        {
            return BadRequest("INVALID_CREDENTIALS_OR_TOTP");
        }

        return Ok("MFA_DISABLED");
    }

    [HttpPost("validate")]
    public async Task<IActionResult> ValidateMfa([FromBody] MfaRequest request)
    {
        var user = await this.GetUser();
        if (user is null)
        {
            return Forbid();
        }

        var isValid = await this._mfaService.UseUserMfa(user, request.TotpCode);
        if (!isValid)
        {
            return BadRequest("INVALID_TOTP");
        }

        return Ok();
    }

    private Task<User?> GetUser()
    {
        return this._dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == User.FindFirst(ClaimTypes.Email)!.Value);
    }
}