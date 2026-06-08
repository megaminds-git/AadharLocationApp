namespace AadharLocation.Shared.DTOs.Admins;

public record UpdateAdminRequest(string Name, string Email, string? NewPassword);
