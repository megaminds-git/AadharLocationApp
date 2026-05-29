using AadharLocation.Shared.Enums;

namespace AadharLocation.Shared.DTOs.Operators;

public record UpdateOperatorRequest(
    string Name,
    string Email,
    string? Phone,
    string? District,
    OperatorStatus Status,
    string? NewTrackerPassword);
