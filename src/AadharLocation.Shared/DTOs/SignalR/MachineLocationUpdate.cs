namespace AadharLocation.Shared.DTOs.SignalR;

public record MachineLocationUpdate(
    int MachineId,
    string MachineName,
    int? OperatorId,
    string? OperatorName,
    double Latitude,
    double Longitude,
    double Accuracy,
    DateTime RecordedAt,
    bool IsWithinGeofence);
