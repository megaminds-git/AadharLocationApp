using AadharLocation.AdminDashboard.Infrastructure;
using AadharLocation.Shared.DTOs.Machines;
using AadharLocation.Shared.DTOs.Operators;
using AadharLocation.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AadharLocation.AdminDashboard.ViewModels;

public partial class AddMachineViewModel : ObservableObject
{
    private readonly ApiClient _api;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _serialNumber = string.Empty;
    [ObservableProperty] private int? _assignedOperatorId;
    [ObservableProperty] private MachineStatus _status = MachineStatus.Idle;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isEditMode;

    public List<MachineStatus> Statuses { get; } = [MachineStatus.Online, MachineStatus.Offline, MachineStatus.Idle];
    public List<OperatorDto> AvailableOperators { get; private set; } = [];

    private int? _editingId;

    public event Action? SaveSucceeded;

    public AddMachineViewModel(ApiClient api) => _api = api;

    public async Task InitForAddAsync()
    {
        IsEditMode       = false;
        _editingId       = null;
        Name             = SerialNumber = string.Empty;
        AssignedOperatorId = null;
        Status           = MachineStatus.Idle;
        ErrorMessage     = string.Empty;
        await LoadOperatorsAsync();
    }

    public async Task InitForEditAsync(MachineDto m)
    {
        IsEditMode         = true;
        _editingId         = m.Id;
        Name               = m.Name;
        SerialNumber       = m.SerialNumber;
        AssignedOperatorId = m.AssignedOperatorId;
        Status             = m.Status;
        ErrorMessage       = string.Empty;
        await LoadOperatorsAsync();
    }

    private async Task LoadOperatorsAsync()
    {
        try
        {
            var result = await _api.GetOperatorsAsync(pageSize: 500);
            var all = result?.Items.ToList() ?? [];
            // Show operators that are unassigned, or already assigned to this machine
            AvailableOperators = all
                .Where(o => o.AssignedMachineId == null || o.AssignedMachineId == _editingId)
                .ToList();
            OnPropertyChanged(nameof(AvailableOperators));
        }
        catch { /* ignore */ }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;
        IsBusy = true;
        try
        {
            if (IsEditMode && _editingId.HasValue)
                await _api.UpdateMachineAsync(_editingId.Value, new UpdateMachineRequest(Name, AssignedOperatorId, Status));
            else
                await _api.CreateMachineAsync(new CreateMachineRequest(Name, SerialNumber, AssignedOperatorId));
            SaveSucceeded?.Invoke();
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsBusy = false; }
    }
}
