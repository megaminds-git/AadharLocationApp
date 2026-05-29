namespace AadharLocation.Shared.DTOs.Machines;

public record CreateMachineRequest(
    string Name,
    string SerialNumber,
    int? AssignedOperatorId);
