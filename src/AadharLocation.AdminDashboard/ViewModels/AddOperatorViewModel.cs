using AadharLocation.AdminDashboard.Infrastructure;
using AadharLocation.Shared.DTOs.Machines;
using AadharLocation.Shared.DTOs.Operators;
using AadharLocation.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AadharLocation.AdminDashboard.ViewModels;

public partial class AddOperatorViewModel : ObservableObject
{
    private readonly ApiClient _api;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _employeeId = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _trackerPassword = string.Empty;
    [ObservableProperty] private int? _assignedMachineId;
    [ObservableProperty] private OperatorStatus _status = OperatorStatus.Active;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isEditMode;

    public List<MachineDto> AvailableMachines { get; private set; } = [];
    public List<OperatorStatus> Statuses { get; } = [OperatorStatus.Active, OperatorStatus.Inactive];

    private int? _editingId;

    public event Action? SaveSucceeded;

    public AddOperatorViewModel(ApiClient api) => _api = api;

    public async Task InitForAddAsync()
    {
        IsEditMode  = false;
        _editingId  = null;
        Name = EmployeeId = Email = Phone = TrackerPassword = string.Empty;
        AssignedMachineId = null;
        Status = OperatorStatus.Active;
        await LoadMachinesAsync();
    }

    public async Task InitForEditAsync(OperatorDto op)
    {
        IsEditMode        = true;
        _editingId        = op.Id;
        Name              = op.Name;
        EmployeeId        = op.EmployeeId;
        Email             = op.Email;
        Phone             = op.Phone ?? string.Empty;
        AssignedMachineId = op.AssignedMachineId;
        Status            = op.Status;
        TrackerPassword   = string.Empty;
        await LoadMachinesAsync();
    }

    private async Task LoadMachinesAsync()
    {
        try
        {
            var result = await _api.GetMachinesAsync(pageSize: 100);
            var all = result?.Items.ToList() ?? [];
            // Only show unassigned machines, plus the one already assigned to this operator (edit mode)
            AvailableMachines = all
                .Where(m => m.AssignedOperatorId == null || m.AssignedOperatorId == _editingId)
                .ToList();
            OnPropertyChanged(nameof(AvailableMachines));
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
            {
                await _api.UpdateOperatorAsync(_editingId.Value, new UpdateOperatorRequest(
                    Name, Email, Phone, AssignedMachineId, Status,
                    string.IsNullOrWhiteSpace(TrackerPassword) ? null : TrackerPassword));
            }
            else
            {
                if (string.IsNullOrWhiteSpace(TrackerPassword))
                {
                    ErrorMessage = "Tracker password is required.";
                    return;
                }
                await _api.CreateOperatorAsync(new CreateOperatorRequest(
                    Name, EmployeeId, Email, Phone, AssignedMachineId, TrackerPassword));
            }
            SaveSucceeded?.Invoke();
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsBusy = false; }
    }
}
