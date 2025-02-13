namespace ImmortalVault_Server.Models;

public class UserSettings
{
    public int Id { get; set; }
    public string Language { get; set; }
    public bool Is12HoursFormat { get; set; }
    public int InactiveMinutes { get; set; }
    public int UserId { get; set; }
}