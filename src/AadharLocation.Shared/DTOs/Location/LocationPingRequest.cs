namespace AadharLocation.Shared.DTOs.Location;

public record LocationPingRequest(
    string DeviceKey,
    int MachineId,
    int OperatorId,
    double Latitude,
    double Longitude,
    double Accuracy,
    DateTime RecordedAt);
