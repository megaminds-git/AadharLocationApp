using System.Windows;
using AadharLocation.AdminDashboard.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AadharLocation.AdminDashboard.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly AuthStateService _auth;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public event Action? LoginSucceeded;

    public LoginViewModel(ApiClient api, AuthStateService auth)
    {
        _api  = api;
        _auth = auth;
    }

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync(string? password)
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Email address is required.";
            return;
        }
        if (string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Password is required.";
            return;
        }

        ErrorMessage = string.Empty;
        IsBusy = true;
        try
        {
            var response = await _api.LoginAsync(Email, password);
            if (response != null)
            {
                _auth.SetSession(response.Token, response.Name, response.Email);
                LoginSucceeded?.Invoke();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message.Contains("401") || ex.Message.Contains("Unauthorized")
                ? "Invalid email or password."
                : $"Connection error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnEmailChanged(string value) => ErrorMessage = string.Empty;

    private bool CanLogin(string? _) => !IsBusy;
}
