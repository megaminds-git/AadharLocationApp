using System.ComponentModel;
using System.Windows;
using AadharLocation.AdminDashboard.ViewModels;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using NetTopologySuite.Geometries;

namespace AadharLocation.AdminDashboard.Views.Dialogs;

public partial class GeofenceEditorDialog : Window
{
    private readonly GeofenceEditorViewModel _vm;
    private readonly WritableLayer _geofenceLayer = new() { Name = "Geofence" };
    private readonly WritableLayer _markerLayer   = new() { Name = "Marker"   };

    public GeofenceEditorDialog(GeofenceEditorViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        _vm = vm;

        void onSaveSucceeded() { DialogResult = true; }
        vm.SaveSucceeded += onSaveSucceeded;
        Closed += (_, _) =>
        {
            vm.SaveSucceeded   -= onSaveSucceeded;
            vm.PropertyChanged -= OnVmPropertyChanged;
        };

        InitMap();
        vm.PropertyChanged += OnVmPropertyChanged;
    }

    // ── Map setup ────────────────────────────────────────────────────────────

    private void InitMap()
    {
        var map = new Map();
        map.Layers.Add(OpenStreetMap.CreateTileLayer());
        map.Layers.Add(_geofenceLayer);
        map.Layers.Add(_markerLayer);
        MapControl.Map = map;

        MapControl.Info += OnMapClicked;

        RefreshOverlay();
        ZoomToGeofence();
    }

    private void OnMapClicked(object? sender, MapInfoEventArgs e)
    {
        if (e.WorldPosition is not MPoint world) return;
        var (lon, lat) = SphericalMercator.ToLonLat(world.X, world.Y);
        _vm.CenterLatitude  = lat;
        _vm.CenterLongitude = lon;
    }

    // ── VM → map sync ─────────────────────────────────────────────────────────

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(GeofenceEditorViewModel.CenterLatitude)
                           or nameof(GeofenceEditorViewModel.CenterLongitude)
                           or nameof(GeofenceEditorViewModel.RadiusMeters))
            Dispatcher.Invoke(RefreshOverlay);
    }

    // ── Drawing ───────────────────────────────────────────────────────────────

    private void RefreshOverlay()
    {
        _geofenceLayer.Clear();
        _markerLayer.Clear();

        var (cx, cy) = SphericalMercator.FromLonLat(_vm.CenterLongitude, _vm.CenterLatitude);

        DrawCircle(cx, cy);
        DrawCenterPin(cx, cy);

        _geofenceLayer.DataHasChanged();
        _markerLayer.DataHasChanged();
    }

    private void DrawCircle(double cx, double cy)
    {
        // Compute circle polygon in geographic space, then project each point
        const int segments = 72;
        double latRad = _vm.CenterLatitude * Math.PI / 180.0;
        double latDeg = _vm.RadiusMeters / 111_320.0;
        double lonDeg = _vm.RadiusMeters / (111_320.0 * Math.Max(Math.Cos(latRad), 0.0001));

        var coords = new Coordinate[segments + 1];
        for (int i = 0; i < segments; i++)
        {
            double angle = 2 * Math.PI * i / segments;
            double lat = _vm.CenterLatitude  + latDeg * Math.Sin(angle);
            double lon = _vm.CenterLongitude + lonDeg * Math.Cos(angle);
            var (x, y) = SphericalMercator.FromLonLat(lon, lat);
            coords[i] = new Coordinate(x, y);
        }
        coords[segments] = coords[0]; // close ring

        var polygon = new GeometryFactory().CreatePolygon(new LinearRing(coords));
        var feature = new GeometryFeature { Geometry = polygon };
        feature.Styles.Add(new VectorStyle
        {
            Fill    = new Brush(new Mapsui.Styles.Color(0x2D, 0xD4, 0xBF, 55)),
            Outline = new Pen(Mapsui.Styles.Color.FromString("#2DD4BF"), 2.5),
        });
        _geofenceLayer.Add(feature);
    }

    private void DrawCenterPin(double cx, double cy)
    {
        var pin = new PointFeature(new MPoint(cx, cy));
        pin.Styles.Add(new SymbolStyle
        {
            SymbolScale = 0.55,
            Fill        = new Brush(Mapsui.Styles.Color.White),
            Outline     = new Pen(Mapsui.Styles.Color.FromString("#2DD4BF"), 2.5),
        });
        _markerLayer.Add(pin);
    }

    private void ZoomToGeofence()
    {
        var (cx, cy) = SphericalMercator.FromLonLat(_vm.CenterLongitude, _vm.CenterLatitude);
        double pad = Math.Max(_vm.RadiusMeters * 3.5, 3_000);
        MapControl.Map.Navigator.ZoomToBox(new MRect(cx - pad, cy - pad, cx + pad, cy + pad));
    }
}
