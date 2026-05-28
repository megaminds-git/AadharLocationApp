namespace AadharLocation.Shared.DTOs.Geofences;

public record CreateGeofenceRequest(
    int MachineId,
    double CenterLatitude,
    double CenterLongitude,
    double RadiusMeters);
