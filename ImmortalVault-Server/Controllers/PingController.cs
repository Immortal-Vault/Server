using Microsoft.AspNetCore.Mvc;

namespace ImmortalVault_Server.Controllers;

[ApiController]
[Route("api/ping")]
public class PingController : ControllerBase
{
    [HttpGet]
    public string Get()
    {
        return "Pong";
    }
}