namespace AadharLocation.Shared.DTOs.Machines;

public record CreateMachineRequest(
    string Name,
    string SerialNumber,
    string Type,
    int? AssignedOperatorId);
