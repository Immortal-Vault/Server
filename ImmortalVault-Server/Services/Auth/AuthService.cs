using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ImmortalVault_Server.Services.Auth;

public interface IAuthService
{
    JwtSecurityToken GenerateAccessToken(string username, string audience);
}

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;

    public AuthService(IConfiguration configuration)
    {
        this._configuration = configuration;
    }
    
    public JwtSecurityToken GenerateAccessToken(string username, string audience)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, username),
        };

        var secretKey = this._configuration[$"JWT:{audience}:SECRET_KEY"] ?? throw new Exception("Secret key not configured");

        var token = new JwtSecurityToken(
            issuer: this._configuration["JWT:ISSUER"],
            audience: this._configuration[$"JWT:{audience}:AUDIENCE"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                SecurityAlgorithms.HmacSha256)
        );

        return token;
    }
}