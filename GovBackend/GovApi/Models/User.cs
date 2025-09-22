namespace GovApi.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public int SearchCredits { get; set; } = 3;
    public DateTime LastReset { get; set; } = DateTime.UtcNow;
}
