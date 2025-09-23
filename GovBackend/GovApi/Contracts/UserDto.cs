namespace GovApi.Contracts;

public record UserDto(int Id, string Email, int SearchCredits, DateTime LastReset);
