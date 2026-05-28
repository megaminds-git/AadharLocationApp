namespace AadharLocation.Shared.DTOs.Auth;

public record LoginResponse(string Token, string Name, string Email, string Role);
