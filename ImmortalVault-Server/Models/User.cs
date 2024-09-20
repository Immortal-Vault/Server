using System.ComponentModel.DataAnnotations;

namespace ImmortalVault_Server.Models;

public class User
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Username is required.")]
    public string Name { get; set; }
    [Required(ErrorMessage = "Email is required.")]
    public string Email { get; set; }
    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; }
    public UserLocalization? UserLocalization { get; set; }
    public UserTokens? UserTokens { get; set; }
}