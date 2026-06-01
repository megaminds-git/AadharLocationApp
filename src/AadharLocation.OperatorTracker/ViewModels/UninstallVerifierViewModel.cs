using AadharLocation.Shared.Constants;
using AadharLocation.Shared.DTOs.Activation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Net.Http;
using System.Net.Http.Json;

namespace AadharLocation.OperatorTracker.ViewModels;

public partial class UninstallVerifierViewModel : ObservableObject
{
    private readonly string _deviceKey;
    private readonly HttpClient _http;

    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isCodeVisible;

    public event EventHandler? VerificationSucceeded;
    public event EventHandler? Cancelled;

    public UninstallVerifierViewModel(string deviceKey, string apiBaseUrl)
    {
        _deviceKey = deviceKey;
        _http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
    }

    [RelayCommand]
    private void ToggleCodeVisibility() => IsCodeVisible = !IsCodeVisible;

    [RelayCommand]
    private async Task VerifyAsync()
    {
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(Code))
        {
            ErrorMessage = "Please enter the uninstall code.";
            return;
        }

        IsBusy = true;
        try
        {
            var response = await _http.PostAsJsonAsync(
                ApiRoutes.Activation.VerifyUninstallCode,
                new VerifyUninstallCodeRequest(_deviceKey, Code));

            if (response.IsSuccessStatusCode)
            {
                VerificationSucceeded?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
                ErrorMessage = body?.Message ?? "Incorrect uninstall code. Please try again.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not connect to server: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke(this, EventArgs.Empty);

    private record ErrorBody(string Message);
}
