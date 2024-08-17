﻿using ImmortalVault_Server.Models;
using ImmortalVault_Server.Services.Auth;
using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImmortalVault_Server.Controllers;

public record SignUpModel(string Username, string Email, string Password);
public record SignInModel(string Email, string Password);

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ApplicationContext _context;

    public AuthController(IAuthService authService, ApplicationContext context)
    {
        this._authService = authService;
        this._context = context;
    }
    
    [HttpPost("signUp")]
    public async Task<IActionResult> SignUp([FromBody] SignUpModel model)
    {
        var sameUser = await this._context.Users
            .Where(u => u.Email == model.Email || u.Name == model.Username)
            .FirstOrDefaultAsync();
        if (sameUser != null)
        {
            return StatusCode(303);
        }
        
        try
        {
            var user = new User
            {
                Name = model.Username,
                Email = model.Email,
                Password = Argon2.Hash(model.Password)
            };
            
            this._context.Users.Add(user);
            await this._context.SaveChangesAsync();

            return Ok();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return StatusCode(500, "An error occurred while creating the user.");
        }
    }
    
    [HttpPost("signIn")]
    public async Task<IActionResult> SignIn([FromBody] SignInModel model)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
        if (user == null)
        {
            return NotFound();
        }
        
        if (!Argon2.Verify(user.Password, model.Password))
        {
            return StatusCode(409);
        }

        var token = this._authService.GenerateAccessToken(user.Name, Audience.ImmortalVaultClient);

        return Ok(new { token });
    }
}