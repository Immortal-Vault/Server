namespace ImmortalVault_Server.Models;

public class UserLocalization
{
    public int Id { get; set; }
    public string Language { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
}