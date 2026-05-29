using AadharLocation.Shared.Enums;

namespace AadharLocation.Shared.DTOs.Operators;

public record OperatorDto(
    int Id,
    string Name,
    string Email,
    string? Phone,
    string? District,
    int? AssignedMachineId,
    string? AssignedMachineName,
    OperatorStatus Status,
    DateTime CreatedAt);
