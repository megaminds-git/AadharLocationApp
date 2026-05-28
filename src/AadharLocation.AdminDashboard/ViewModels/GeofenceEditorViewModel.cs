using AadharLocation.AdminDashboard.Infrastructure;
using AadharLocation.Shared.DTOs.Geofences;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AadharLocation.AdminDashboard.ViewModels;

public partial class GeofenceEditorViewModel : ObservableObject
{
    private readonly ApiClient _api;

    [ObservableProperty] private int _machineId;
    [ObservableProperty] private string _machineName = string.Empty;
    [ObservableProperty] private double _centerLatitude;
    [ObservableProperty] private double _centerLongitude;
    [ObservableProperty] private double _radiusMeters = 500;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private GeofenceDto? _existingGeofence;

    public event Action? SaveSucceeded;

    public GeofenceEditorViewModel(ApiClient api) => _api = api;

    public async Task InitAsync(int machineId, string machineName, double defaultLat, double defaultLon)
    {
        MachineId       = machineId;
        MachineName     = machineName;
        CenterLatitude  = defaultLat;
        CenterLongitude = defaultLon;

        try
        {
            var fences = await _api.GetGeofencesAsync(machineId);
            ExistingGeofence = fences?.FirstOrDefault(f => f.IsActive);
            if (ExistingGeofence != null)
            {
                CenterLatitude  = ExistingGeofence.CenterLatitude;
                CenterLongitude = ExistingGeofence.CenterLongitude;
                RadiusMeters    = ExistingGeofence.RadiusMeters;
            }
        }
        catch { /* no existing geofence */ }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;
        IsBusy = true;
        try
        {
            if (ExistingGeofence != null)
                await _api.DeleteGeofenceAsync(ExistingGeofence.Id);

            await _api.CreateGeofenceAsync(new CreateGeofenceRequest(
                MachineId, CenterLatitude, CenterLongitude, RadiusMeters));

            SaveSucceeded?.Invoke();
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsBusy = false; }
    }
}
