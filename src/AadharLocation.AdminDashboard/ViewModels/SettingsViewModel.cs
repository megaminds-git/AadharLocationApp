using System.Collections.ObjectModel;
using AadharLocation.AdminDashboard.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AadharLocation.AdminDashboard.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ApiClient _api;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string _newRecipientEmail = string.Empty;
    [ObservableProperty] private string _recipientStatusMessage = string.Empty;

    public ObservableCollection<string> AlertEmailRecipients { get; } = [];

    public SettingsViewModel(ApiClient api)
    {
        _api = api;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var settings = await _api.GetSettingsAsync();
            if (settings != null)
            {
                AlertEmailRecipients.Clear();
                var recipientsStr = settings.GetValueOrDefault("AlertEmailRecipients", "");
                foreach (var email in recipientsStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    AlertEmailRecipients.Add(email);
            }
        }
        catch (Exception ex) { StatusMessage = $"Load failed: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void AddRecipient()
    {
        RecipientStatusMessage = string.Empty;
        var email = NewRecipientEmail.Trim();

        if (string.IsNullOrEmpty(email)) return;

        if (!email.Contains('@') || email.IndexOf('.', email.IndexOf('@')) < 0)
        {
            RecipientStatusMessage = "Please enter a valid email address.";
            return;
        }

        if (AlertEmailRecipients.Contains(email, StringComparer.OrdinalIgnoreCase))
        {
            RecipientStatusMessage = "This email is already in the list.";
            return;
        }

        AlertEmailRecipients.Add(email);
        NewRecipientEmail = string.Empty;
    }

    [RelayCommand]
    private void RemoveRecipient(string? email)
    {
        if (email is null) return;
        AlertEmailRecipients.Remove(email);
        RecipientStatusMessage = string.Empty;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var settings = new Dictionary<string, string>
            {
                ["AlertEmailRecipients"] = string.Join(",", AlertEmailRecipients),
            };
            await _api.SaveSettingsAsync(settings);
            StatusMessage = "Settings saved successfully.";
        }
        catch (Exception ex) { StatusMessage = $"Save failed: {ex.Message}"; }
        finally { IsBusy = false; }
    }
}
