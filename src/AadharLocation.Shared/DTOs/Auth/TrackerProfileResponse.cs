using AadharLocation.Shared.Enums;

namespace AadharLocation.Shared.DTOs.Auth;

public record TrackerProfileResponse(
    int OperatorId,
    string Name,
    string Email,
    string? Phone,
    string? District,
    int MachineId,
    string MachineName,
    string SerialNumber,
    string MachineType,
    MachineStatus MachineStatus,
    double? GeofenceLat,
    double? GeofenceLon,
    double? GeofenceRadius
);
