using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using AadharLocation.AdminDashboard.Infrastructure;
using AadharLocation.AdminDashboard.ViewModels;
using Microsoft.Extensions.Options;
using Microsoft.Web.WebView2.Core;

namespace AadharLocation.AdminDashboard.Views.Dialogs;

public partial class GeofenceEditorDialog : Window
{
    private readonly GeofenceEditorViewModel _vm;
    private readonly string _apiKey;
    private bool _mapReady = false;
    private bool _suppressOverlayUpdate = false;

    public GeofenceEditorDialog(GeofenceEditorViewModel vm, IOptions<GoogleMapsOptions> mapsOptions)
    {
        InitializeComponent();
        DataContext = vm;
        _vm     = vm;
        _apiKey = mapsOptions.Value.ApiKey;

        void onSaveSucceeded() { DialogResult = true; }
        vm.SaveSucceeded += onSaveSucceeded;
        Closed += (_, _) =>
        {
            vm.SaveSucceeded   -= onSaveSucceeded;
            vm.PropertyChanged -= OnVmPropertyChanged;
            MapWebView.Dispose();
        };

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await MapWebView.EnsureCoreWebView2Async();
        MapWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
        LoadMapHtml();
        _vm.PropertyChanged += OnVmPropertyChanged;
    }

    private void LoadMapHtml()
    {
        var html    = HtmlLoader.Load("geofence_editor.html").Replace("{{API_KEY}}", _apiKey);
        var tmpDir  = Path.Combine(Path.GetTempPath(), "AadharLocationMaps");
        Directory.CreateDirectory(tmpDir);
        var tmpFile = Path.Combine(tmpDir, "geofence_editor.html");
        File.WriteAllText(tmpFile, html, System.Text.Encoding.UTF8);

        MapWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "maps.aadhar.local", tmpDir, CoreWebView2HostResourceAccessKind.Allow);
        MapWebView.CoreWebView2.Navigate("https://maps.aadhar.local/geofence_editor.html");
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
                    SendInitMap();
                    break;

                case "MapClicked":
                case "MarkerDragged":
                    var lat = doc.RootElement.GetProperty("latitude").GetDouble();
                    var lon = doc.RootElement.GetProperty("longitude").GetDouble();
                    Dispatcher.Invoke(() =>
                    {
                        _suppressOverlayUpdate = true;
                        _vm.CenterLatitude  = lat;
                        _vm.CenterLongitude = lon;
                        _suppressOverlayUpdate = false;
                        SendUpdateOverlay();
                    });
                    break;
            }
        }
        catch { /* ignore malformed messages */ }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressOverlayUpdate) return;
        if (e.PropertyName is nameof(GeofenceEditorViewModel.CenterLatitude)
                           or nameof(GeofenceEditorViewModel.CenterLongitude)
                           or nameof(GeofenceEditorViewModel.RadiusMeters))
            Dispatcher.Invoke(SendUpdateOverlay);
    }

    private void SendInitMap()
    {
        SendMessage(new
        {
            type         = "InitMap",
            theme        = GetTheme(),
            latitude     = _vm.CenterLatitude,
            longitude    = _vm.CenterLongitude,
            radiusMeters = _vm.RadiusMeters
        });
        SendMessage(new { type = "ZoomToGeofence" });
    }

    private void SendUpdateOverlay()
    {
        if (!_mapReady) return;
        SendMessage(new
        {
            type         = "UpdateOverlay",
            latitude     = _vm.CenterLatitude,
            longitude    = _vm.CenterLongitude,
            radiusMeters = _vm.RadiusMeters
        });
    }

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
}
