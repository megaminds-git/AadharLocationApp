using AadharLocation.AdminDashboard.Infrastructure;
using AadharLocation.Shared.DTOs.Operators;
using AadharLocation.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections;
using System.ComponentModel;

namespace AadharLocation.AdminDashboard.ViewModels;

public partial class AddOperatorViewModel : ObservableObject, INotifyDataErrorInfo
{
    private readonly ApiClient _api;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _district = string.Empty;
    [ObservableProperty] private string _trackerPassword = string.Empty;
    [ObservableProperty] private OperatorStatus _status = OperatorStatus.Active;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private bool _passwordHasError;

    public List<OperatorStatus> Statuses { get; } = [OperatorStatus.Active, OperatorStatus.Inactive];

    private int? _editingId;
    private bool _validationActive;
    private readonly Dictionary<string, string[]> _fieldErrors = new();

    public bool HasErrors => _validationActive && _fieldErrors.Count > 0;
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
    public event Action? SaveSucceeded;

    public AddOperatorViewModel(ApiClient api) => _api = api;

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
        SetFieldError(nameof(Email), string.IsNullOrWhiteSpace(Email));
    }

    partial void OnNameChanged(string value)
    {
        if (_validationActive) SetFieldError(nameof(Name), string.IsNullOrWhiteSpace(value));
    }

    partial void OnEmailChanged(string value)
    {
        if (_validationActive) SetFieldError(nameof(Email), string.IsNullOrWhiteSpace(value));
    }

    public Task InitForAddAsync()
    {
        IsEditMode = false;
        _editingId = null;
        _validationActive = false;
        _fieldErrors.Clear();
        Name = Email = Phone = District = TrackerPassword = string.Empty;
        Status = OperatorStatus.Active;
        ErrorMessage = string.Empty;
        return Task.CompletedTask;
    }

    public Task InitForEditAsync(OperatorDto op)
    {
        IsEditMode      = true;
        _editingId      = op.Id;
        _validationActive = false;
        _fieldErrors.Clear();
        Name            = op.Name;
        Email           = op.Email;
        Phone           = op.Phone ?? string.Empty;
        District        = op.District ?? string.Empty;
        Status          = op.Status;
        TrackerPassword = string.Empty;
        ErrorMessage    = string.Empty;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;

        _validationActive = true;
        ValidateFields();
        if (HasErrors) return;

        if (!IsEditMode && string.IsNullOrWhiteSpace(TrackerPassword))
        {
            PasswordHasError = true;
            ErrorMessage = "Password is required.";
            return;
        }
        PasswordHasError = false;

        IsBusy = true;
        try
        {
            if (IsEditMode && _editingId.HasValue)
            {
                await _api.UpdateOperatorAsync(_editingId.Value, new UpdateOperatorRequest(
                    Name, Email,
                    string.IsNullOrWhiteSpace(Phone) ? null : Phone,
                    string.IsNullOrWhiteSpace(District) ? null : District,
                    Status,
                    string.IsNullOrWhiteSpace(TrackerPassword) ? null : TrackerPassword));
            }
            else
            {
                await _api.CreateOperatorAsync(new CreateOperatorRequest(
                    Name, Email,
                    string.IsNullOrWhiteSpace(Phone) ? null : Phone,
                    string.IsNullOrWhiteSpace(District) ? null : District,
                    TrackerPassword));
            }
            SaveSucceeded?.Invoke();
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsBusy = false; }
    }
}
