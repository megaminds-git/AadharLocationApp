using AadharLocation.Shared.Enums;

namespace AadharLocation.Shared.DTOs.Machines;

public record UpdateMachineRequest(
    string Name,
    int? AssignedOperatorId,
    MachineStatus Status);
