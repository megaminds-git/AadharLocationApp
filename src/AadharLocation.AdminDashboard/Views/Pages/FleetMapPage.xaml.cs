using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using AadharLocation.AdminDashboard.Infrastructure;
using AadharLocation.AdminDashboard.ViewModels;
using Microsoft.Extensions.Options;
using Microsoft.Web.WebView2.Core;

namespace AadharLocation.AdminDashboard.Views.Pages;

public partial class FleetMapPage : UserControl
{
    private readonly FleetMapViewModel _vm;
    private readonly string _apiKey;
    private bool _mapReady = false;
    private bool _webViewInitialized = false;
    private List<MapMachinePin>? _pendingPins = null;

    public FleetMapPage(FleetMapViewModel vm, IOptions<GoogleMapsOptions> mapsOptions)
    {
        InitializeComponent();
        _vm     = vm;
        _apiKey = mapsOptions.Value.ApiKey;
        DataContext = vm;

        vm.PinsLoaded        += OnPinsLoaded;
        vm.PinUpdated        += OnPinUpdated;
        vm.PlaceFound        += OnPlaceFound;
        vm.PinFocusRequested += OnPinFocusRequested;
        vm.FilterChanged     += OnFilterChanged;

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_webViewInitialized) return;
        _webViewInitialized = true;

        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AadharLocation", "WebView2Cache");
        var env = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
        await MapWebView.EnsureCoreWebView2Async(env);
        MapWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
        LoadMapHtml();
    }

    private void LoadMapHtml()
    {
        var html    = HtmlLoader.Load("fleet_map.html").Replace("{{API_KEY}}", _apiKey);
        var tmpDir  = Path.Combine(Path.GetTempPath(), "AadharLocationMaps");
        Directory.CreateDirectory(tmpDir);
        var tmpFile = Path.Combine(tmpDir, "fleet_map.html");
        File.WriteAllText(tmpFile, html, System.Text.Encoding.UTF8);

        MapWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "maps.aadhar.local", tmpDir, CoreWebView2HostResourceAccessKind.Allow);
        MapWebView.CoreWebView2.Navigate("https://maps.aadhar.local/fleet_map.html");
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var json = e.TryGetWebMessageAsString();
        if (json is null) return;

        try
        {
            var doc  = JsonDocument.Parse(json);
            var type = doc.RootElement.GetProperty("type").GetString();

            switch (type)
            {
                case "MapReady":
                    _mapReady = true;
                    SendMessage(new { type = "InitMap", theme = GetTheme() });
                    if (_pendingPins is not null)
                    {
                        SendLoadPins(_pendingPins);
                        _pendingPins = null;
                        ApplyCurrentFilter();
                    }
                    break;

                case "MarkerClicked":
                    var machineId = doc.RootElement.GetProperty("machineId").GetInt32();
                    var pin = _vm.Pins.FirstOrDefault(p => p.MachineId == machineId);
                    if (pin is not null)
                        Application.Current.Dispatcher.InvokeAsync(() => _vm.SelectPinCommand.Execute(pin));
                    break;
            }
        }
        catch { /* ignore malformed messages */ }
    }

    private void OnPinsLoaded(List<MapMachinePin> pins)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_mapReady)
            {
                SendLoadPins(pins);
                ApplyCurrentFilter();
            }
            else _pendingPins = pins;
        });
    }

    private void OnFilterChanged(int[]? visibleIds)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (!_mapReady) return;
            SendMessage(new { type = "SetVisiblePins", machineIds = visibleIds });
        });
    }

    private void ApplyCurrentFilter()
    {
        if (!_mapReady) return;
        SendMessage(new { type = "SetVisiblePins", machineIds = _vm.GetVisibleMachineIds() });
    }

    private void OnPinUpdated(MapMachinePin pin)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (!_mapReady) return;
            SendMessage(new { type = "UpdatePin", pin = SerializePin(pin) });
        });
    }

    private void OnPlaceFound(double lat, double lon)
    {
        Application.Current.Dispatcher.Invoke(() =>
            SendMessage(new { type = "FocusLocation", latitude = lat, longitude = lon }));
    }

    private void OnPinFocusRequested(MapMachinePin pin)
    {
        Application.Current.Dispatcher.Invoke(() =>
            SendMessage(new { type = "FocusPin", machineId = pin.MachineId, latitude = pin.Latitude, longitude = pin.Longitude }));
    }

    private void SendLoadPins(List<MapMachinePin> pins)
    {
        SendMessage(new { type = "LoadPins", pins = pins.Select(SerializePin).ToArray() });
    }

    private static object SerializePin(MapMachinePin p) => new
    {
        machineId        = p.MachineId,
        machineName      = p.MachineName,
        operatorName     = p.OperatorName,
        latitude         = p.Latitude,
        longitude        = p.Longitude,
        status           = p.Status.ToString(),
        isWithinGeofence = p.IsWithinGeofence,
        city             = p.City
    };

    private void SendMessage(object payload)
    {
        if (MapWebView.CoreWebView2 is null) return;
        MapWebView.CoreWebView2.PostWebMessageAsString(JsonSerializer.Serialize(payload));
    }

    private static string GetTheme()
    {
        var theme = Application.Current.Resources["ThemeMode"] as string;
        return theme == "Dark" ? "dark" : "light";
    }

    public async Task ActivateAsync()
    {
        await _vm.LoadAsync();
    }
}
