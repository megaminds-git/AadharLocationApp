using System.Windows;
using System.Windows.Controls;
using AadharLocation.AdminDashboard.ViewModels;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using CommunityToolkit.Mvvm.Input;

namespace AadharLocation.AdminDashboard.Views.Pages;

public partial class FleetMapPage : UserControl
{
    private readonly FleetMapViewModel _vm;
    private readonly WritableLayer _markerLayer = new() { Name = "Machines" };
    private readonly Dictionary<int, PointFeature> _features = [];

    public FleetMapPage(FleetMapViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        vm.PinsLoaded  += OnPinsLoaded;
        vm.PinUpdated  += OnPinUpdated;

        InitMap();
    }

    private void InitMap()
    {
        var map = new Map();
        map.Layers.Add(OpenStreetMap.CreateTileLayer());
        map.Layers.Add(_markerLayer);
        MapControl.Map = map;
    }

    public async Task ActivateAsync()
    {
        await _vm.LoadAsync();
    }

    private void OnPinsLoaded(List<MapMachinePin> pins)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _features.Clear();
            _markerLayer.Clear();
            foreach (var p in pins) AddOrUpdateFeature(p);
            _markerLayer.DataHasChanged();
            ZoomToFit();
        });
    }

    private void OnPinUpdated(MapMachinePin pin)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            AddOrUpdateFeature(pin);
            _markerLayer.DataHasChanged();
        });
    }

    private void AddOrUpdateFeature(MapMachinePin pin)
    {
        var spherical = SphericalMercator.FromLonLat(pin.Longitude, pin.Latitude);
        var mpoint    = new MPoint(spherical.x, spherical.y);

        var color = pin.Status switch
        {
            Shared.Enums.MachineStatus.Online  => Mapsui.Styles.Color.FromString("#4ADE80"),
            Shared.Enums.MachineStatus.Offline => Mapsui.Styles.Color.FromString("#F87171"),
            _                                   => Mapsui.Styles.Color.FromString("#FBBF24"),
        };

        if (_features.TryGetValue(pin.MachineId, out var existing))
        {
            _markerLayer.TryRemove(existing);
        }

        var feature = new PointFeature(mpoint);
        feature.Styles.Add(MakeSymbol(color, pin.MachineName));
        _features[pin.MachineId] = feature;
        _markerLayer.Add(feature);
    }

    private static IStyle MakeSymbol(Mapsui.Styles.Color fill, string label) =>
        new SymbolStyle
        {
            SymbolScale       = 0.6,
            Fill              = new Mapsui.Styles.Brush(fill),
            Outline           = new Pen(Mapsui.Styles.Color.White, 2),
            MaxVisible        = double.MaxValue,
            MinVisible        = double.MinValue,
        };

    private void ZoomToFit()
    {
        if (_features.Count == 0) return;
        var points = _features.Values.Select(f => f.Point).ToList();
        var minX = points.Min(p => p.X);
        var maxX = points.Max(p => p.X);
        var minY = points.Min(p => p.Y);
        var maxY = points.Max(p => p.Y);
        var extent = new MRect(minX - 5000, minY - 5000, maxX + 5000, maxY + 5000);
        MapControl.Map.Navigator.ZoomToBox(extent);
    }
}
