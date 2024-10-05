using System.Text;
using ImmortalVault_Server;
using ImmortalVault_Server.Middlewares;
using ImmortalVault_Server.Services.Auth;
using ImmortalVault_Server.Services.Client;
using ImmortalVault_Server.Services.GoogleDrive;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var configuration = new ConfigurationBuilder()
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["JWT:ISSUER"],
            ValidAudience = configuration["JWT:IMMORTAL_VAULT_CLIENT:AUDIENCE"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                configuration["JWT:IMMORTAL_VAULT_CLIENT:SECRET_KEY"] ??
                throw new Exception("Secret key not configured")))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies["immortalVaultJwtToken"];
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddSingleton<IClientService, ClientService>();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IGoogleDriveService, GoogleDriveService>();

var connection = builder.Configuration.GetConnectionString("DefaultConnection") ??
                 throw new Exception("Database connection string not found");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connection));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(corsPolicyBuilder =>
{
    corsPolicyBuilder
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();

    var allowedOrigins = configuration.GetSection("CORS:ALLOWED_ORIGINS").Get<string[]>();
    if (allowedOrigins is null)
    {
        throw new Exception("Allowed origins not found");
    }

    corsPolicyBuilder.WithOrigins(allowedOrigins);
});

app.UseHttpsRedirection();

app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.None,
    HttpOnly = HttpOnlyPolicy.Always,
    Secure = CookieSecurePolicy.Always
});

app.UseMiddleware<JwtRefreshMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();