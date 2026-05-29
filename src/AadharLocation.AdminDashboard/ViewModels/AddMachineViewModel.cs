using AadharLocation.AdminDashboard.Infrastructure;
using AadharLocation.Shared.DTOs.Machines;
using AadharLocation.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AadharLocation.AdminDashboard.ViewModels;

public partial class AddMachineViewModel : ObservableObject
{
    private readonly ApiClient _api;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _serialNumber = string.Empty;
    [ObservableProperty] private string _type = string.Empty;
    [ObservableProperty] private MachineStatus _status = MachineStatus.Idle;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isEditMode;

    public List<MachineStatus> Statuses { get; } = [MachineStatus.Online, MachineStatus.Offline, MachineStatus.Idle];
    public List<string> MachineTypes { get; } = ["Excavator", "Bulldozer", "Crane", "Forklift", "Truck", "Other"];

    private int? _editingId;

    public event Action? SaveSucceeded;

    public AddMachineViewModel(ApiClient api) => _api = api;

    public Task InitForAddAsync()
    {
        IsEditMode = false;
        _editingId = null;
        Name = SerialNumber = Type = string.Empty;
        Status = MachineStatus.Idle;
        ErrorMessage = string.Empty;
        return Task.CompletedTask;
    }

    public Task InitForEditAsync(MachineDto m)
    {
        IsEditMode   = true;
        _editingId   = m.Id;
        Name         = m.Name;
        SerialNumber = m.SerialNumber;
        Type         = m.Type;
        Status       = m.Status;
        ErrorMessage = string.Empty;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;
        IsBusy = true;
        try
        {
            if (IsEditMode && _editingId.HasValue)
                await _api.UpdateMachineAsync(_editingId.Value, new UpdateMachineRequest(Name, Type, Status));
            else
                await _api.CreateMachineAsync(new CreateMachineRequest(Name, SerialNumber, Type));
            SaveSucceeded?.Invoke();
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsBusy = false; }
    }
}
