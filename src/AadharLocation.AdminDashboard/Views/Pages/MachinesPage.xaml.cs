using System.Windows.Controls;
using AadharLocation.AdminDashboard.ViewModels;
using AadharLocation.AdminDashboard.Views.Dialogs;
using AadharLocation.Shared.DTOs.Machines;

namespace AadharLocation.AdminDashboard.Views.Pages;

public partial class MachinesPage : UserControl
{
    private readonly MachinesViewModel _vm;
    private readonly AddMachineViewModel _addVm;
    private readonly GeofenceEditorViewModel _geoVm;

    public MachinesPage(MachinesViewModel vm, AddMachineViewModel addVm, GeofenceEditorViewModel geoVm)
    {
        InitializeComponent();
        _vm    = vm;
        _addVm = addVm;
        _geoVm = geoVm;
        DataContext = vm;

        vm.AddRequested      += OnAddRequested;
        vm.EditRequested     += OnEditRequested;
        vm.GeofenceRequested += OnGeofenceRequested;
    }

    public async Task ActivateAsync() => await _vm.LoadAsync();

    private async void OnAddRequested()
    {
        await _addVm.InitForAddAsync();
        var dialog = new AddMachineDialog(_addVm);
        if (dialog.ShowDialog() == true) await _vm.LoadAsync();
    }

    private async void OnEditRequested(MachineDto? m)
    {
        if (m == null) return;
        await _addVm.InitForEditAsync(m);
        var dialog = new AddMachineDialog(_addVm);
        if (dialog.ShowDialog() == true) await _vm.LoadAsync();
    }

    private async void OnGeofenceRequested(MachineDto m)
    {
        double defLat = m.CurrentLatitude  ?? 28.6139;
        double defLon = m.CurrentLongitude ?? 77.2090;
        await _geoVm.InitAsync(m.Id, m.Name, defLat, defLon);
        var dialog = new GeofenceEditorDialog(_geoVm);
        dialog.ShowDialog();
    }
}
