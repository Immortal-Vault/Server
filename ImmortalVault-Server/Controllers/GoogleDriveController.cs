using System.Security.Claims;
using ImmortalVault_Server.Services.GoogleDrive;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImmortalVault_Server.Controllers;

public record GoogleDriveUploadRequest(string Content);

[ApiController]
[Route("api/googleDrive/secretFile")]
public class GoogleDriveController : ControllerBase
{
    private readonly IGoogleDriveService _googleDriveService;
    private readonly ApplicationDbContext _dbContext;

    public GoogleDriveController(IGoogleDriveService googleDriveService, ApplicationDbContext dbContext)
    {
        this._googleDriveService = googleDriveService;
        this._dbContext = dbContext;
    }
    
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> UploadSecretFile([FromBody] GoogleDriveUploadRequest request)
    {
        var user = await this._dbContext.Users
            .Include(user => user.UserTokens)
            .FirstOrDefaultAsync(u => u.Email == User.FindFirst(ClaimTypes.Email)!.Value);
        if (user is null)
        {
            return NotFound();
        }

        if (this._googleDriveService.IsTokenExpired(user))
        {
            await this._googleDriveService.UpdateTokens(user);
        }
        
        await this._googleDriveService.UploadOrReplaceSecretFile(user, request.Content);
        
        return Ok();
    }
    
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetPasswordFile()
    {
        var user = await this._dbContext.Users
            .Include(user => user.UserTokens)
            .FirstOrDefaultAsync(u => u.Email == User.FindFirst(ClaimTypes.Email)!.Value);
        if (user is null)
        {
            return NotFound("Account not found");
        }

        if (this._googleDriveService.IsTokenExpired(user))
        {
            await this._googleDriveService.UpdateTokens(user);
        }

        var fileContent = await this._googleDriveService.GetSecretFile(user);
        if (fileContent is null)
        {
            return NotFound("Secret file not found");
        }
        
        return Ok(fileContent.Value.Content);
    }
}