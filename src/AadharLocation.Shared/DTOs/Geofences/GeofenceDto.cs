namespace AadharLocation.Shared.DTOs.Geofences;

public record GeofenceDto(
    int Id,
    int MachineId,
    string MachineName,
    double CenterLatitude,
    double CenterLongitude,
    double RadiusMeters,
    bool IsActive,
    DateTime CreatedAt);
