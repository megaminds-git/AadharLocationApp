namespace AadharLocation.Shared.DTOs.SignalR;

public record MachineOfflineEvent(
    int MachineId,
    string MachineName,
    DateTime LastSeenAt,
    int MinutesOffline);
