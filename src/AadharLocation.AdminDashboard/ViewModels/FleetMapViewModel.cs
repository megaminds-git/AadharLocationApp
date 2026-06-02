using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using AadharLocation.AdminDashboard.Infrastructure;
using AadharLocation.Shared.DTOs.Machines;
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
    private readonly IHttpClientFactory _httpFactory;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private MapMachinePin? _selectedPin;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isSearching;

    public ObservableCollection<MapMachinePin> Pins { get; } = [];

    [ObservableProperty] private string _selectedPinAddress = string.Empty;
    [ObservableProperty] private bool _isLoadingAddress;

    public event Action<MapMachinePin>?      PinUpdated;
    public event Action<List<MapMachinePin>>? PinsLoaded;
    public event Action<double, double>?     PlaceFound;
    public event Action<MapMachinePin>?      PinFocusRequested;

    [RelayCommand]
    private async Task SelectPin(MapMachinePin? pin)
    {
        SelectedPin = pin;
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
            var http = _httpFactory.CreateClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("AadharLocationApp/1.0");
            var url = $"https://nominatim.openstreetmap.org/reverse?lat={pin.Latitude.ToString(CultureInfo.InvariantCulture)}&lon={pin.Longitude.ToString(CultureInfo.InvariantCulture)}&format=json";
            var json = await http.GetStringAsync(url);
            var result = JsonSerializer.Deserialize<NominatimReverseResult>(json);
            SelectedPinAddress = result?.display_name ?? string.Empty;

            var city = ExtractCity(result?.address);
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
                var city = await FetchCityAsync(pin.Latitude, pin.Longitude);
                if (!string.IsNullOrEmpty(city))
                    await UpdatePinCityAsync(pin.MachineId, city);
            }
            catch (Exception ex) { StatusMessage = $"City lookup failed: {ex.Message}"; }
            await Task.Delay(1100); // Nominatim rate limit: 1 req/sec
        }
    }

    private async Task<string> FetchCityAsync(double lat, double lon)
    {
        var http = _httpFactory.CreateClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("AadharLocationApp/1.0");
        var url = $"https://nominatim.openstreetmap.org/reverse?lat={lat.ToString(CultureInfo.InvariantCulture)}&lon={lon.ToString(CultureInfo.InvariantCulture)}&format=json";
        var json = await http.GetStringAsync(url);
        var result = JsonSerializer.Deserialize<NominatimReverseResult>(json);
        return ExtractCity(result?.address);
    }

    private static string ExtractCity(NominatimAddress? addr) =>
        addr?.city
        ?? addr?.town
        ?? addr?.municipality
        ?? addr?.city_district
        ?? addr?.county
        ?? addr?.village
        ?? addr?.suburb
        ?? string.Empty;

    private Task UpdatePinCityAsync(int machineId, string city)
    {
        var idx = -1;
        MapMachinePin? current = null;
        for (var i = 0; i < Pins.Count; i++)
        {
            if (Pins[i].MachineId != machineId) continue;
            idx = i;
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

    public FleetMapViewModel(ApiClient api, SignalRClient signalR, IHttpClientFactory httpFactory)
    {
        _api         = api;
        _signalR     = signalR;
        _httpFactory = httpFactory;
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
                _ = LoadCitiesAsync();
            }
        }
        catch (Exception ex) { StatusMessage = $"Map load error: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private void OnLocationUpdate(MachineLocationUpdate u)
    {
        var existing = Pins.FirstOrDefault(p => p.MachineId == u.MachineId);
        var updated  = new MapMachinePin(u.MachineId, u.MachineName, u.OperatorName,
            u.Latitude, u.Longitude, Shared.Enums.MachineStatus.Online, u.IsWithinGeofence,
            existing?.City ?? string.Empty);

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

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;
        IsSearching = true;
        StatusMessage = string.Empty;
        try
        {
            var http = _httpFactory.CreateClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("AadharLocationApp/1.0");
            var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(SearchText)}&format=json&limit=1";
            var json = await http.GetStringAsync(url);
            var results = JsonSerializer.Deserialize<NominatimResult[]>(json);
            if (results is { Length: > 0 })
            {
                PlaceFound?.Invoke(double.Parse(results[0].lat), double.Parse(results[0].lon));
                StatusMessage = results[0].display_name;
            }
            else
            {
                StatusMessage = "Place not found.";
            }
        }
        catch { StatusMessage = "Search failed. Check your connection."; }
        finally { IsSearching = false; }
    }

    private record NominatimResult(
        [property: JsonPropertyName("lat")] string lat,
        [property: JsonPropertyName("lon")] string lon,
        [property: JsonPropertyName("display_name")] string display_name);

    private record NominatimReverseResult(
        [property: JsonPropertyName("display_name")] string display_name,
        [property: JsonPropertyName("address")] NominatimAddress? address);

    private record NominatimAddress(
        [property: JsonPropertyName("city")]          string? city,
        [property: JsonPropertyName("town")]          string? town,
        [property: JsonPropertyName("municipality")]  string? municipality,
        [property: JsonPropertyName("city_district")] string? city_district,
        [property: JsonPropertyName("county")]        string? county,
        [property: JsonPropertyName("village")]       string? village,
        [property: JsonPropertyName("suburb")]        string? suburb);
}
