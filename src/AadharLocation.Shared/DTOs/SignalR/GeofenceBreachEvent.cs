namespace AadharLocation.Shared.DTOs.SignalR;

public record GeofenceBreachEvent(
    int AlertId,
    int MachineId,
    string MachineName,
    int OperatorId,
    string OperatorName,
    double Latitude,
    double Longitude,
    double DistanceMeters,
    DateTime OccurredAt);
