using System.Windows;
using System.Windows.Controls;
using AadharLocation.AdminDashboard.Infrastructure;
using AadharLocation.AdminDashboard.ViewModels;
using AadharLocation.AdminDashboard.Views.Dialogs;
using AadharLocation.Shared.DTOs.Machines;
using Microsoft.Extensions.Options;

namespace AadharLocation.AdminDashboard.Views.Pages;

public partial class MachinesPage : UserControl
{
    private readonly MachinesViewModel _vm;
    private readonly AddMachineViewModel _addVm;
    private readonly GeofenceEditorViewModel _geoVm;
    private readonly IOptions<GoogleMapsOptions> _mapsOptions;

    public MachinesPage(MachinesViewModel vm, AddMachineViewModel addVm, GeofenceEditorViewModel geoVm, IOptions<GoogleMapsOptions> mapsOptions)
    {
        InitializeComponent();
        _vm          = vm;
        _addVm       = addVm;
        _geoVm       = geoVm;
        _mapsOptions = mapsOptions;
        DataContext  = vm;

        vm.AddRequested      += OnAddRequested;
        vm.EditRequested     += OnEditRequested;
        vm.GeofenceRequested += OnGeofenceRequested;
        vm.ConfirmDelete      = name =>
        {
            var dlg = new ConfirmDeleteDialog(name) { Owner = Window.GetWindow(this) };
            return dlg.ShowDialog() == true;
        };
    }

    public async Task ActivateAsync() => await _vm.LoadAsync();

    private async void OnAddRequested()
    {
        await _addVm.InitForAddAsync();
        var dialog = new AddMachineDialog(_addVm) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) await _vm.LoadAsync();
    }

    private async void OnEditRequested(MachineDto? m)
    {
        if (m == null) return;
        await _addVm.InitForEditAsync(m);
        var dialog = new AddMachineDialog(_addVm) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) await _vm.LoadAsync();
    }

    private async void OnGeofenceRequested(MachineDto m)
    {
        double defLat = m.CurrentLatitude  ?? 28.6139;
        double defLon = m.CurrentLongitude ?? 77.2090;
        await _geoVm.InitAsync(m.Id, m.Name, defLat, defLon);
        var dialog = new GeofenceEditorDialog(_geoVm, _mapsOptions) { Owner = Window.GetWindow(this) };
        dialog.ShowDialog();
    }
}
