namespace ImmortalVault_Server.Middlewares;

public class ClientVersionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string? _expectedClientVersion;

    public ClientVersionMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        this._next = next;
        this._expectedClientVersion = configuration["EXPECTED_WEBSITE_VERSION"];
        if (this._expectedClientVersion is null)
        {
            throw new Exception("Excepted website version not configured");
        }
    }

    public async Task Invoke(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("Client-Version", out var clientVersion))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Client version header is required.");
            return;
        }
        
        if (clientVersion != this._expectedClientVersion)
        {
            context.Response.StatusCode = StatusCodes.Status426UpgradeRequired;
            await context.Response.WriteAsync(
                $"Client version mismatch. Expected: {this._expectedClientVersion}, Received: {clientVersion}");
            return;
        }
        
        await this._next(context);
    }
}