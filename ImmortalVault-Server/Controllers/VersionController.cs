using ImmortalVault_Server.Services.Client;
using Microsoft.AspNetCore.Mvc;

namespace ImmortalVault_Server.Controllers;

[ApiController]
[Route("api/version")]
public class VersionController
{
    private readonly IClientService _clientService;
    private readonly IConfiguration _configuration;

    public VersionController(IClientService clientService, IConfiguration configuration)
    {
        this._clientService = clientService;
        this._configuration = configuration;
    }

    [HttpGet("downloadUrl")]
    public async Task<IActionResult> GetDownloadUrl()
    {
        var repositoryOwner = this._configuration["REPOSITORY_OWNER"];
        var repositoryName = this._configuration["REPOSITORY_NAME"];

        if (repositoryOwner is null || repositoryName is null)
        {
            return new StatusCodeResult(500);
        }

        var version = await this._clientService.GetLatestVersion(repositoryOwner, repositoryName);

        if (version is null)
        {
            return new StatusCodeResult(500);
        }

        var downloadUrl = this._clientService.BuildDownloadUrl(version, repositoryOwner, repositoryName);

        return new JsonResult(new { downloadUrl, version = version.Replace("v", "") });
    }
}