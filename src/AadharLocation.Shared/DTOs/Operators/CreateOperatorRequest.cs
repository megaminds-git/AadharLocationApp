namespace AadharLocation.Shared.DTOs.Operators;

public record CreateOperatorRequest(
    string Name,
    string EmployeeId,
    string Email,
    string? Phone,
    int? AssignedMachineId);
