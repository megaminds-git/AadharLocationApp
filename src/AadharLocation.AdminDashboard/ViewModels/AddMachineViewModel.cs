using AadharLocation.AdminDashboard.Infrastructure;
using AadharLocation.Shared.DTOs.Machines;
using AadharLocation.Shared.DTOs.Operators;
using AadharLocation.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections;
using System.ComponentModel;

namespace AadharLocation.AdminDashboard.ViewModels;

public record OperatorSelectionItem(OperatorDto? Operator, bool IsOccupied)
{
    public static readonly OperatorSelectionItem Unassigned = new(null, false);
}

public partial class AddMachineViewModel : ObservableObject, INotifyDataErrorInfo
{
    private readonly ApiClient _api;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _serialNumber = string.Empty;
    [ObservableProperty] private int? _assignedOperatorId;
    [ObservableProperty] private OperatorSelectionItem _selectedOperatorItem = OperatorSelectionItem.Unassigned;

    partial void OnSelectedOperatorItemChanged(OperatorSelectionItem value)
        => AssignedOperatorId = value?.Operator?.Id;
    [ObservableProperty] private MachineStatus _status = MachineStatus.Idle;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isEditMode;

    public List<MachineStatus> Statuses { get; } = [MachineStatus.Online, MachineStatus.Offline, MachineStatus.Idle];
    public List<OperatorSelectionItem> AvailableOperators { get; private set; } = [];

    private int? _editingId;
    private bool _validationActive;
    private readonly Dictionary<string, string[]> _fieldErrors = new();

    public bool HasErrors => _validationActive && _fieldErrors.Count > 0;
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
    public event Action? SaveSucceeded;

    public AddMachineViewModel(ApiClient api) => _api = api;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (!_validationActive) return Array.Empty<string>();
        if (propertyName == null) return _fieldErrors.Values.SelectMany(e => e);
        return _fieldErrors.TryGetValue(propertyName, out var errs) ? errs : Array.Empty<string>();
    }

    private void SetFieldError(string prop, bool hasError)
    {
        var had = _fieldErrors.ContainsKey(prop);
        if (hasError) _fieldErrors[prop] = ["Required"];
        else _fieldErrors.Remove(prop);
        if (had != hasError)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(prop));
            OnPropertyChanged(nameof(HasErrors));
        }
    }

    private void ValidateFields()
    {
        SetFieldError(nameof(Name), string.IsNullOrWhiteSpace(Name));
        if (!IsEditMode)
            SetFieldError(nameof(SerialNumber), string.IsNullOrWhiteSpace(SerialNumber));
    }

    partial void OnNameChanged(string value)
    {
        if (_validationActive) SetFieldError(nameof(Name), string.IsNullOrWhiteSpace(value));
    }

    partial void OnSerialNumberChanged(string value)
    {
        if (_validationActive && !IsEditMode)
            SetFieldError(nameof(SerialNumber), string.IsNullOrWhiteSpace(value));
    }

    public async Task InitForAddAsync()
    {
        IsEditMode            = false;
        _editingId            = null;
        _validationActive     = false;
        _fieldErrors.Clear();
        Name                  = SerialNumber = string.Empty;
        AssignedOperatorId    = null;
        Status                = MachineStatus.Idle;
        ErrorMessage          = string.Empty;
        await LoadOperatorsAsync();
        SelectedOperatorItem  = OperatorSelectionItem.Unassigned;
    }

    public async Task InitForEditAsync(MachineDto m)
    {
        IsEditMode            = true;
        _editingId            = m.Id;
        _validationActive     = false;
        _fieldErrors.Clear();
        Name                  = m.Name;
        SerialNumber          = m.SerialNumber;
        AssignedOperatorId    = m.AssignedOperatorId;
        Status                = m.Status;
        ErrorMessage          = string.Empty;
        await LoadOperatorsAsync();
        SelectedOperatorItem  = m.AssignedOperatorId.HasValue
            ? AvailableOperators.FirstOrDefault(x => x.Operator?.Id == m.AssignedOperatorId) ?? OperatorSelectionItem.Unassigned
            : OperatorSelectionItem.Unassigned;
    }

    private async Task LoadOperatorsAsync()
    {
        try
        {
            var result = await _api.GetOperatorsAsync(pageSize: 500);
            var all = result?.Items.ToList() ?? [];
            AvailableOperators = [
                OperatorSelectionItem.Unassigned,
                ..all.Select(o => new OperatorSelectionItem(o,
                    o.AssignedMachineId != null && o.AssignedMachineId != _editingId))
            ];
            OnPropertyChanged(nameof(AvailableOperators));
        }
        catch { /* ignore */ }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;

        _validationActive = true;
        ValidateFields();
        if (HasErrors) return;

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
