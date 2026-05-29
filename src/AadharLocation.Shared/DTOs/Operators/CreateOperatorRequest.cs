namespace AadharLocation.Shared.DTOs.Operators;

public record CreateOperatorRequest(
    string Name,
    string Email,
    string? Phone,
    string? District,
    string? TrackerPassword);
