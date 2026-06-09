using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using AadharLocation.AdminDashboard.Infrastructure;
using AadharLocation.Shared.DTOs.SignalR;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AadharLocation.AdminDashboard.ViewModels;

public record MapMachinePin(int MachineId, string MachineName, string? OperatorName,
    double Latitude, double Longitude, Shared.Enums.MachineStatus Status, bool IsWithinGeofence, string City = "");

public partial class FleetMapViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly SignalRClient _signalR;
    private readonly IGeocodingService _geocoding;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isMapLoading = true;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public bool IsMapOrDataLoading => IsLoading || IsMapLoading;

    partial void OnIsLoadingChanged(bool value)    => OnPropertyChanged(nameof(IsMapOrDataLoading));
    partial void OnIsMapLoadingChanged(bool value) => OnPropertyChanged(nameof(IsMapOrDataLoading));
    [ObservableProperty] private MapMachinePin? _selectedPin;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isSearching;
    [ObservableProperty] private string _selectedPinAddress = string.Empty;
    [ObservableProperty] private bool _isLoadingAddress;
    [ObservableProperty] private string _machineFilterText = string.Empty;

    private string _appliedFilter = string.Empty;

    public ObservableCollection<MapMachinePin> Pins { get; } = [];
    public ICollectionView FilteredPins { get; }

    public string MachineCountText
    {
        get
        {
            var total = Pins.Count;
            if (string.IsNullOrWhiteSpace(_appliedFilter))
                return $"{total} machines tracked";
            var visible = FilteredPins.Cast<MapMachinePin>().Count();
            return $"{visible} of {total} machines";
        }
    }

    public int[]? GetVisibleMachineIds() =>
        string.IsNullOrWhiteSpace(_appliedFilter)
            ? null
            : FilteredPins.Cast<MapMachinePin>().Select(p => p.MachineId).ToArray();

    public event Action<MapMachinePin>?       PinUpdated;
    public event Action<List<MapMachinePin>>? PinsLoaded;
    public event Action<double, double>?      PlaceFound;
    public event Action<MapMachinePin>?       PinFocusRequested;
    public event Action<int[]?>?              FilterChanged;

    public FleetMapViewModel(ApiClient api, SignalRClient signalR, IGeocodingService geocoding)
    {
        _api       = api;
        _signalR   = signalR;
        _geocoding = geocoding;

        _signalR.MachineLocationUpdated += OnLocationUpdate;
        _signalR.MachineWentOffline     += OnMachineOffline;
        _signalR.MachineOnline          += OnMachineOnline;

        FilteredPins        = CollectionViewSource.GetDefaultView(Pins);
        FilteredPins.Filter = FilterMachinePin;
        Pins.CollectionChanged += (_, _) => OnPropertyChanged(nameof(MachineCountText));
    }

    private bool FilterMachinePin(object obj)
    {
        if (obj is not MapMachinePin pin) return false;
        if (string.IsNullOrWhiteSpace(_appliedFilter)) return true;
        return pin.MachineName.Contains(_appliedFilter, StringComparison.OrdinalIgnoreCase)
            || (pin.OperatorName?.Contains(_appliedFilter, StringComparison.OrdinalIgnoreCase) ?? false)
            || pin.City.Contains(_appliedFilter, StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand]
    private void ApplyMachineFilter()
    {
        _appliedFilter = MachineFilterText.Trim();
        FilteredPins.Refresh();
        OnPropertyChanged(nameof(MachineCountText));
        FilterChanged?.Invoke(GetVisibleMachineIds());
    }

    [RelayCommand]
    private void ClearMachineFilter()
    {
        MachineFilterText = string.Empty;
        _appliedFilter    = string.Empty;
        FilteredPins.Refresh();
        OnPropertyChanged(nameof(MachineCountText));
        FilterChanged?.Invoke(null);
    }

    [RelayCommand]
    private async Task SelectPin(MapMachinePin? pin)
    {
        SelectedPin        = pin;
        SelectedPinAddress = string.Empty;
        if (pin == null) return;
        PinFocusRequested?.Invoke(pin);
        await ReverseGeocodeAsync(pin);
    }

    private async Task ReverseGeocodeAsync(MapMachinePin pin)
    {
        IsLoadingAddress = true;
        try
        {
            SelectedPinAddress = await _geocoding.GetAddressAsync(pin.Latitude, pin.Longitude);
            var city = await _geocoding.GetCityAsync(pin.Latitude, pin.Longitude);
            if (!string.IsNullOrEmpty(city))
                await UpdatePinCityAsync(pin.MachineId, city);
        }
        catch { SelectedPinAddress = string.Empty; }
        finally { IsLoadingAddress = false; }
    }

    private async Task LoadCitiesAsync()
    {
        foreach (var pin in Pins.ToList())
        {
            if (!string.IsNullOrEmpty(pin.City)) continue;
            try
            {
                var city = await _geocoding.GetCityAsync(pin.Latitude, pin.Longitude);
                if (!string.IsNullOrEmpty(city))
                    await UpdatePinCityAsync(pin.MachineId, city);
            }
            catch (Exception ex) { StatusMessage = $"City lookup failed: {ex.Message}"; }
        }
    }

    private Task UpdatePinCityAsync(int machineId, string city)
    {
        var idx     = -1;
        MapMachinePin? current = null;
        for (var i = 0; i < Pins.Count; i++)
        {
            if (Pins[i].MachineId != machineId) continue;
            idx     = i;
            current = Pins[i];
            break;
        }
        if (idx < 0 || current == null) return Task.CompletedTask;

        var updated = current with { City = city };
        Pins[idx] = updated;
        PinUpdated?.Invoke(updated);

        if (SelectedPin?.MachineId == machineId)
            SelectedPin = updated;

        return Task.CompletedTask;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading     = true;
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
                        m.CurrentLatitude!.Value, m.CurrentLongitude!.Value, m.Status, m.IsWithinGeofence ?? true))
                    .ToList();

                foreach (var p in pins) Pins.Add(p);
                PinsLoaded?.Invoke(pins);
                _ = LoadCitiesAsync();
            }
        }
        catch (Exception ex) { StatusMessage = $"Map load error: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;
        IsSearching   = true;
        StatusMessage = string.Empty;
        try
        {
            var result = await _geocoding.SearchPlaceAsync(SearchText);
            if (result.HasValue)
            {
                PlaceFound?.Invoke(result.Value.Lat, result.Value.Lon);
                StatusMessage = SearchText;
            }
            else
            {
                StatusMessage = "Place not found.";
            }
        }
        catch { StatusMessage = "Search failed. Check your connection."; }
        finally { IsSearching = false; }
    }

    private void OnLocationUpdate(MachineLocationUpdate u)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var existing = Pins.FirstOrDefault(p => p.MachineId == u.MachineId);
            var updated  = new MapMachinePin(u.MachineId, u.MachineName, u.OperatorName,
                u.Latitude, u.Longitude, Shared.Enums.MachineStatus.Online, u.IsWithinGeofence,
                existing?.City ?? string.Empty);

            if (existing != null) Pins[Pins.IndexOf(existing)] = updated;
            else Pins.Add(updated);

            PinUpdated?.Invoke(updated);
        });
    }

    private void OnMachineOffline(MachineOfflineEvent e)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var existing = Pins.FirstOrDefault(p => p.MachineId == e.MachineId);
            if (existing == null) return;
            var updated = existing with { Status = Shared.Enums.MachineStatus.Offline };
            Pins[Pins.IndexOf(existing)] = updated;
            PinUpdated?.Invoke(updated);
            if (SelectedPin?.MachineId == e.MachineId) SelectedPin = updated;
        });
    }

    private void OnMachineOnline(int machineId, string machineName)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var existing = Pins.FirstOrDefault(p => p.MachineId == machineId);
            if (existing == null) return;
            var updated = existing with { Status = Shared.Enums.MachineStatus.Online };
            Pins[Pins.IndexOf(existing)] = updated;
            PinUpdated?.Invoke(updated);
            if (SelectedPin?.MachineId == machineId) SelectedPin = updated;
        });
    }
}
