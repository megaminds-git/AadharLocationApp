using AadharLocation.AdminDashboard.Infrastructure;
using AadharLocation.Shared.DTOs.Admins;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections;
using System.ComponentModel;

namespace AadharLocation.AdminDashboard.ViewModels;

public partial class AddAdminViewModel : ObservableObject, INotifyDataErrorInfo
{
    private readonly ApiClient _api;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isEditMode;

    public string PasswordHint => IsEditMode ? "New password (leave blank to keep)" : "Password *";
    partial void OnIsEditModeChanged(bool value) => OnPropertyChanged(nameof(PasswordHint));

    private int? _editingId;
    private bool _validationActive;
    private readonly Dictionary<string, string[]> _fieldErrors = new();

    public bool HasErrors => _validationActive && _fieldErrors.Count > 0;
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
    public event Action? SaveSucceeded;

    public AddAdminViewModel(ApiClient api) => _api = api;

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
        if (!IsEditMode)
            SetFieldError(nameof(Password), string.IsNullOrWhiteSpace(Password));
    }

    partial void OnNameChanged(string value)
    {
        if (_validationActive) SetFieldError(nameof(Name), string.IsNullOrWhiteSpace(value));
    }

    partial void OnEmailChanged(string value)
    {
        if (_validationActive) SetFieldError(nameof(Email), string.IsNullOrWhiteSpace(value));
    }

    partial void OnPasswordChanged(string value)
    {
        if (_validationActive && !IsEditMode)
            SetFieldError(nameof(Password), string.IsNullOrWhiteSpace(value));
    }

    public Task InitForAddAsync()
    {
        IsEditMode        = false;
        _editingId        = null;
        _validationActive = false;
        _fieldErrors.Clear();
        Name = Email = Password = string.Empty;
        ErrorMessage = string.Empty;
        return Task.CompletedTask;
    }

    public Task InitForEditAsync(AdminDto admin)
    {
        IsEditMode        = true;
        _editingId        = admin.Id;
        _validationActive = false;
        _fieldErrors.Clear();
        Name     = admin.Name;
        Email    = admin.Email;
        Password = string.Empty;
        ErrorMessage = string.Empty;
        return Task.CompletedTask;
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
            {
                await _api.UpdateAdminAsync(_editingId.Value, new UpdateAdminRequest(
                    Name, Email,
                    string.IsNullOrWhiteSpace(Password) ? null : Password));
            }
            else
            {
                await _api.CreateAdminAsync(new CreateAdminRequest(Name, Email, Password));
            }
            SaveSucceeded?.Invoke();
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsBusy = false; }
    }
}
