using Microsoft.AspNetCore.Mvc;

namespace ImmortalVault_Server.Controllers;

[ApiController]
[Route("api")]
public class PingController : ControllerBase
{
    [HttpGet]
    public string Get()
    {
        return "Immortal Vault Server";
    }
    
    [HttpGet("ping")]
    public string Ping()
    {
        return "Pong";
    }
}