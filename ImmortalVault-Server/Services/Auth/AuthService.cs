using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ImmortalVault_Server.Models;
using Isopoh.Cryptography.Argon2;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ImmortalVault_Server.Services.Auth;

public interface IAuthService
{
    string GenerateAccessToken(string email, string audience, int expirationMinutes);
    Task<bool> ChangePassword(User user, string newPassword);
}

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _dbContext;

    public AuthService(IConfiguration configuration, ApplicationDbContext dbContext)
    {
        this._configuration = configuration;
        this._dbContext = dbContext;
    }

    public string GenerateAccessToken(string email, string audience, int expirationMinutes)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email),
        };

        var secretKey = this._configuration[$"JWT:{audience}:SECRET_KEY"] ??
                        throw new Exception("Secret key not configured");

        var token = new JwtSecurityToken(
            issuer: this._configuration["JWT:ISSUER"],
            audience: this._configuration[$"JWT:{audience}:AUDIENCE"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<bool> ChangePassword(User user, string newPassword)
    {
        var password = Argon2.Hash(newPassword);

        if (Argon2.Verify(user.Password, password))
        {
            return false;
        }

        var updateResult = await this._dbContext.Users.Where(u => u.Id == user.Id).ExecuteUpdateAsync(
            t => t
                .SetProperty(u => u.Password, password));

        return updateResult > 0;
    }
}