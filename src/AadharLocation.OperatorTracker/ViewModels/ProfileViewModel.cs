using AadharLocation.Shared.Constants;
using AadharLocation.Shared.DTOs.Auth;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AadharLocation.OperatorTracker.Services;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AadharLocation.OperatorTracker.ViewModels;

public partial class ProfileViewModel : ObservableObject, IDisposable
{
    private readonly IProfileService _profileService;
    private readonly IActivationService _activation;
    private readonly LocationSenderService _sender;
    private readonly IGpsService _gps;
    private readonly IHttpClientFactory _httpFactory;

    public event EventHandler? LogoutRequested;

    [ObservableProperty] private string _operatorName = "Loading...";
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _district = string.Empty;
    [ObservableProperty] private string _machineName = string.Empty;
    [ObservableProperty] private string _serialNumber = string.Empty;
    [ObservableProperty] private string _machineType = string.Empty;
    [ObservableProperty] private string _machineStatus = string.Empty;
    [ObservableProperty] private string _geofenceLat = "—";
    [ObservableProperty] private string _geofenceLon = "—";
    [ObservableProperty] private string _geofenceRadius = "—";
    [ObservableProperty] private string _currentLat = "Fetching...";
    [ObservableProperty] private string _currentLon = "Fetching...";
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isSending;

    public ProfileViewModel(
        IProfileService profileService,
        IActivationService activation,
        LocationSenderService sender,
        IGpsService gps,
        IHttpClientFactory httpFactory)
    {
        _profileService = profileService;
        _activation = activation;
        _sender = sender;
        _gps = gps;
        _httpFactory = httpFactory;
        _sender.PingSent += OnPingSent;
    }

    public async Task LoadAsync()
    {
        var profile = await _profileService.GetProfileAsync();
        if (profile is not null)
        {
            OperatorName = profile.Name;
            Email = profile.Email;
            Phone = profile.Phone ?? "—";
            District = profile.District ?? "—";
            MachineName = profile.MachineName;
            SerialNumber = profile.SerialNumber;
            MachineType = profile.MachineType;
            MachineStatus = profile.MachineStatus.ToString();
            GeofenceLat = profile.GeofenceLat?.ToString("F5") ?? "Not set";
            GeofenceLon = profile.GeofenceLon?.ToString("F5") ?? "Not set";
            GeofenceRadius = profile.GeofenceRadius.HasValue
                ? $"{profile.GeofenceRadius:F0} m"
                : "Not set";
        }

        var gps = await _gps.GetLocationAsync(CancellationToken.None);
        CurrentLat = gps?.Latitude.ToString("F5") ?? "Unavailable";
        CurrentLon = gps?.Longitude.ToString("F5") ?? "Unavailable";
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendNowAsync()
    {
        IsSending = true;
        StatusMessage = "Sending...";
        try
        {
            var sent = await _sender.SendNowAsync();
            if (!sent)
                StatusMessage = "Could not read location — please wait and try again.";
        }
        catch
        {
            StatusMessage = "Send failed.";
        }
        finally
        {
            IsSending = false;
        }
    }

    [RelayCommand]
    private void Logout()
    {
        var credentials = _activation.GetCredentials();
        if (credentials is not null)
            _ = SendLogoutEventAsync(credentials.Token, credentials.DeviceKey);

        _activation.ClearCredentials();
        LogoutRequested?.Invoke(this, EventArgs.Empty);
    }

    private async Task SendLogoutEventAsync(string token, string deviceKey)
    {
        try
        {
            var http = _httpFactory.CreateClient("Api");
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            await http.PostAsJsonAsync(ApiRoutes.Auth.TrackerLogout, new TrackerLogoutRequest(deviceKey));
        }
        catch { /* fire-and-forget; do not block logout on network failure */ }
    }

    private bool CanSend() => !IsSending;
    partial void OnIsSendingChanged(bool value) => SendNowCommand.NotifyCanExecuteChanged();

    private void OnPingSent(object? sender, string msg) => StatusMessage = msg;

    public void Dispose() => _sender.PingSent -= OnPingSent;
}
