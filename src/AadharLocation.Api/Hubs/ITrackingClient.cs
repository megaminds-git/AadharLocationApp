using AadharLocation.Shared.DTOs.SignalR;

namespace AadharLocation.Api.Hubs;

public interface ITrackingClient
{
    Task MachineLocationUpdated(MachineLocationUpdate update);
    Task GeofenceBreachDetected(GeofenceBreachEvent breach);
    Task MachineOffline(MachineOfflineEvent offlineEvent);
    Task MachineOnline(int machineId, string machineName);
    Task AlertAcknowledged(int alertId);
}
