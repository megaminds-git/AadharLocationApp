using AadharLocation.Shared.Enums;

namespace AadharLocation.Shared.DTOs.Machines;

public record MachineDto(
    int Id,
    string Name,
    string SerialNumber,
    string Type,
    int? AssignedOperatorId,
    string? AssignedOperatorName,
    double? CurrentLatitude,
    double? CurrentLongitude,
    DateTime? LastSeenAt,
    MachineStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? MachineAuthCode,
    bool IsDeleted);
