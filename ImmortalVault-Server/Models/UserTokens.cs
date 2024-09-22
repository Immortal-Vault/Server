namespace ImmortalVault_Server.Models;

public class UserTokens
{
    public int Id { get; set; }
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
}