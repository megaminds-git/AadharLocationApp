using AadharLocation.Shared.Enums;

namespace AadharLocation.Shared.DTOs.Operators;

public record OperatorDto(
    int Id,
    string Name,
    string EmployeeId,
    string Email,
    string? Phone,
    int? AssignedMachineId,
    string? AssignedMachineName,
    OperatorStatus Status,
    DateTime CreatedAt);
