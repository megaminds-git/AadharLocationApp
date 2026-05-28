using System.Collections.ObjectModel;
using AadharLocation.AdminDashboard.Infrastructure;
using AadharLocation.Shared.DTOs.Machines;
using AadharLocation.Shared.DTOs.SignalR;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AadharLocation.AdminDashboard.ViewModels;

public record MapMachinePin(int MachineId, string MachineName, string? OperatorName,
    double Latitude, double Longitude, Shared.Enums.MachineStatus Status, bool IsWithinGeofence);

public partial class FleetMapViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly SignalRClient _signalR;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private MapMachinePin? _selectedPin;

    public ObservableCollection<MapMachinePin> Pins { get; } = [];

    public event Action<MapMachinePin>?      PinUpdated;
    public event Action<List<MapMachinePin>>? PinsLoaded;

    [RelayCommand]
    private void SelectPin(MapMachinePin? pin) => SelectedPin = pin;

    public FleetMapViewModel(ApiClient api, SignalRClient signalR)
    {
        _api     = api;
        _signalR = signalR;
        _signalR.MachineLocationUpdated += OnLocationUpdate;
        _signalR.MachineWentOffline     += OnMachineOffline;
        _signalR.MachineOnline          += OnMachineOnline;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            var machines = await _api.GetLiveMachinesAsync();
            if (machines != null)
            {
                Pins.Clear();
                var pins = machines
                    .Where(m => m.CurrentLatitude.HasValue && m.CurrentLongitude.HasValue)
                    .Select(m => new MapMachinePin(m.Id, m.Name, m.AssignedOperatorName,
                        m.CurrentLatitude!.Value, m.CurrentLongitude!.Value, m.Status, true))
                    .ToList();

                foreach (var p in pins) Pins.Add(p);
                PinsLoaded?.Invoke(pins);
            }
        }
        catch (Exception ex) { StatusMessage = $"Map load error: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private void OnLocationUpdate(MachineLocationUpdate u)
    {
        var existing = Pins.FirstOrDefault(p => p.MachineId == u.MachineId);
        var updated  = new MapMachinePin(u.MachineId, u.MachineName, u.OperatorName,
            u.Latitude, u.Longitude, Shared.Enums.MachineStatus.Online, u.IsWithinGeofence);

        if (existing != null) Pins[Pins.IndexOf(existing)] = updated;
        else Pins.Add(updated);

        PinUpdated?.Invoke(updated);
    }

    private void OnMachineOffline(MachineOfflineEvent e)
    {
        var existing = Pins.FirstOrDefault(p => p.MachineId == e.MachineId);
        if (existing == null) return;
        var updated = existing with { Status = Shared.Enums.MachineStatus.Offline };
        Pins[Pins.IndexOf(existing)] = updated;
        PinUpdated?.Invoke(updated);
    }

    private void OnMachineOnline(int machineId, string machineName)
    {
        var existing = Pins.FirstOrDefault(p => p.MachineId == machineId);
        if (existing == null) return;
        var updated = existing with { Status = Shared.Enums.MachineStatus.Online };
        Pins[Pins.IndexOf(existing)] = updated;
        PinUpdated?.Invoke(updated);
    }
}
