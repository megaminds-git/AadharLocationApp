namespace AadharLocation.OperatorTracker.Infrastructure;

public record TrackerCredentials(
    string Token,
    string DeviceKey,
    int OperatorId,
    int MachineId);
