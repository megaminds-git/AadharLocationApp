using AadharLocation.AdminDashboard.Infrastructure;
using AadharLocation.Shared.DTOs.Operators;
using AadharLocation.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AadharLocation.AdminDashboard.ViewModels;

public partial class AddOperatorViewModel : ObservableObject
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

    public List<OperatorStatus> Statuses { get; } = [OperatorStatus.Active, OperatorStatus.Inactive];

    private int? _editingId;

    public event Action? SaveSucceeded;

    public AddOperatorViewModel(ApiClient api) => _api = api;

    public Task InitForAddAsync()
    {
        IsEditMode = false;
        _editingId = null;
        Name = Email = Phone = District = TrackerPassword = string.Empty;
        Status = OperatorStatus.Active;
        ErrorMessage = string.Empty;
        return Task.CompletedTask;
    }

    public Task InitForEditAsync(OperatorDto op)
    {
        IsEditMode      = true;
        _editingId      = op.Id;
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
                if (string.IsNullOrWhiteSpace(TrackerPassword))
                {
                    ErrorMessage = "Password is required.";
                    return;
                }
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
