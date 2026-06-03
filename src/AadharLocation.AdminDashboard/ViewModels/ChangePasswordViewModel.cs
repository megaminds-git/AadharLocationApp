using System.Net.Http;
using AadharLocation.AdminDashboard.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AadharLocation.AdminDashboard.ViewModels;

public partial class ChangePasswordViewModel : ObservableObject
{
    private readonly ApiClient _api;

    [ObservableProperty] private string _currentPassword = string.Empty;
    [ObservableProperty] private string _newPassword = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isError;
    [ObservableProperty] private bool _isBusy;

    public event Action? CloseRequested;

    public ChangePasswordViewModel(ApiClient api)
    {
        _api = api;
    }

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        StatusMessage = string.Empty;
        IsError = false;

        if (string.IsNullOrWhiteSpace(CurrentPassword) ||
            string.IsNullOrWhiteSpace(NewPassword) ||
            string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            StatusMessage = "All fields are required.";
            IsError = true;
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            StatusMessage = "New password and confirmation do not match.";
            IsError = true;
            return;
        }

        if (NewPassword.Length < 6)
        {
            StatusMessage = "New password must be at least 6 characters.";
            IsError = true;
            return;
        }

        IsBusy = true;
        try
        {
            await _api.ChangePasswordAsync(CurrentPassword, NewPassword);
            StatusMessage = "Password changed successfully.";
            IsError = false;
            await Task.Delay(800);
            CloseRequested?.Invoke();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            StatusMessage = "Current password is incorrect.";
            IsError = true;
        }
        catch
        {
            StatusMessage = "Failed to change password. Please try again.";
            IsError = true;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
