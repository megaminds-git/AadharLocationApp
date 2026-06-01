using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using AadharLocation.AdminDashboard.Infrastructure;
using AadharLocation.Shared.DTOs.Activation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;

namespace AadharLocation.AdminDashboard.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly AuthStateService _auth;
    private readonly string _serverConfigPath;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isDarkTheme;

    // Server connection
    [ObservableProperty] private string _serverUrl = string.Empty;
    [ObservableProperty] private string _serverStatusMessage = string.Empty;

    // Email settings
    [ObservableProperty] private string _smtpHost = string.Empty;
    [ObservableProperty] private string _smtpPort = "587";
    [ObservableProperty] private string _smtpUser = string.Empty;
    [ObservableProperty] private string _fromAddress = string.Empty;
    [ObservableProperty] private string _adminRecipients = string.Empty;

    // Timing settings
    [ObservableProperty] private string _offlineThresholdMinutes = "5";
    [ObservableProperty] private string _geofenceCooldownMinutes = "5";

    // Device management
    [ObservableProperty] private string _deviceStatusMessage = string.Empty;
    public ObservableCollection<DeviceDto> Devices { get; } = [];

    public event Action<string>? UninstallCodeGenerated;

    public SettingsViewModel(ApiClient api, AuthStateService auth, IConfiguration config)
    {
        _api  = api;
        _auth = auth;
        _isDarkTheme = auth.IsDarkTheme;

        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AadharLocation");
        _serverConfigPath = Path.Combine(appDataDir, "server_config.json");

        _serverUrl = (config["ApiBaseUrl"] ?? "http://localhost:5163").TrimEnd('/');
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var settings = await _api.GetSettingsAsync();
            if (settings != null)
            {
                SmtpHost                = settings.GetValueOrDefault("SmtpHost", string.Empty);
                SmtpPort                = settings.GetValueOrDefault("SmtpPort", "587");
                SmtpUser                = settings.GetValueOrDefault("SmtpUser", string.Empty);
                FromAddress             = settings.GetValueOrDefault("FromAddress", string.Empty);
                AdminRecipients         = settings.GetValueOrDefault("AdminRecipients", string.Empty);
                OfflineThresholdMinutes = settings.GetValueOrDefault("OfflineThresholdMinutes", "5");
                GeofenceCooldownMinutes = settings.GetValueOrDefault("GeofenceCooldownMinutes", "5");
            }
        }
        catch (Exception ex) { StatusMessage = $"Load failed: {ex.Message}"; }
        finally { IsBusy = false; }

        await LoadDevicesAsync();
    }

    [RelayCommand]
    public async Task LoadDevicesAsync()
    {
        DeviceStatusMessage = string.Empty;
        try
        {
            var devices = await _api.GetDevicesAsync();
            Devices.Clear();
            if (devices != null)
                foreach (var d in devices) Devices.Add(d);
        }
        catch (Exception ex) { DeviceStatusMessage = $"Could not load devices: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task GenerateCodeAsync(DeviceDto? device)
    {
        if (device is null) return;
        DeviceStatusMessage = string.Empty;
        try
        {
            var result = await _api.GenerateUninstallCodeAsync(device.DeviceKey);
            if (result != null)
                UninstallCodeGenerated?.Invoke(result.Code);
            await LoadDevicesAsync();
        }
        catch (Exception ex) { DeviceStatusMessage = $"Generate failed: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task DeactivateDeviceAsync(DeviceDto? device)
    {
        if (device is null) return;
        DeviceStatusMessage = string.Empty;
        try
        {
            await _api.DeactivateDeviceAsync(device.DeviceKey);
            DeviceStatusMessage = $"Device for {device.MachineName} deactivated.";
            await LoadDevicesAsync();
        }
        catch (Exception ex) { DeviceStatusMessage = $"Deactivate failed: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var settings = new Dictionary<string, string>
            {
                ["SmtpHost"]                = SmtpHost,
                ["SmtpPort"]                = SmtpPort,
                ["SmtpUser"]                = SmtpUser,
                ["FromAddress"]             = FromAddress,
                ["AdminRecipients"]         = AdminRecipients,
                ["OfflineThresholdMinutes"] = OfflineThresholdMinutes,
                ["GeofenceCooldownMinutes"] = GeofenceCooldownMinutes,
            };
            await _api.SaveSettingsAsync(settings);
            StatusMessage = "Settings saved successfully.";
        }
        catch (Exception ex) { StatusMessage = $"Save failed: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        _auth.SetTheme(IsDarkTheme);
        App.SwitchTheme(IsDarkTheme);
    }

    [RelayCommand]
    private void SaveServerUrl()
    {
        var url = ServerUrl.Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(url))
        {
            ServerStatusMessage = "URL cannot be empty.";
            return;
        }
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_serverConfigPath)!);
            File.WriteAllText(_serverConfigPath,
                JsonSerializer.Serialize(new { ApiBaseUrl = url }, new JsonSerializerOptions { WriteIndented = false }));
            ServerStatusMessage = "Saved. Restart the app to connect to the new server.";
        }
        catch (Exception ex) { ServerStatusMessage = $"Save failed: {ex.Message}"; }
    }
}
