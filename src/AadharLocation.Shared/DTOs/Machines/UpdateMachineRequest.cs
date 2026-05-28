using AadharLocation.Shared.Enums;

namespace AadharLocation.Shared.DTOs.Machines;

public record UpdateMachineRequest(
    string Name,
    string Type,
    int? AssignedOperatorId,
    MachineStatus Status);
