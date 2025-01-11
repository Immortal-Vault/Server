using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ImmortalVault_Server.Models.Serializers;

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
    public UserSettings UserSettings { get; set; }
    public UserTokens? UserTokens { get; set; }
    [JsonIgnore] public string? Mfa { get; set; }
    public bool MfaEnabled => Mfa is not null;

    [JsonConverter(typeof(ListSerializer<string>))]
    public List<string> MfaRecoveryCodes { get; set; }
}