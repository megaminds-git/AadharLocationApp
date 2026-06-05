using System.Collections.ObjectModel;
using AadharLocation.AdminDashboard.Infrastructure;
using AadharLocation.Shared.DTOs.Admins;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AadharLocation.AdminDashboard.ViewModels;

public partial class AdminsViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly AuthStateService _auth;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public string CurrentUserEmail => _auth.UserEmail;

    public ObservableCollection<AdminDto> Admins { get; } = [];

    public event Action? AddRequested;
    public event Action<AdminDto>? EditRequested;
    public Func<string, bool>? ConfirmDelete { get; set; }

    public AdminsViewModel(ApiClient api, AuthStateService auth)
    {
        _api  = api;
        _auth = auth;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _api.GetAdminsAsync();
            if (result != null)
            {
                Admins.Clear();
                foreach (var a in result) Admins.Add(a);
            }
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void AddAdmin() => AddRequested?.Invoke();

    [RelayCommand]
    private void EditAdmin(AdminDto? admin)
    {
        if (admin is not null) EditRequested?.Invoke(admin);
    }

    [RelayCommand]
    private async Task DeleteAdminAsync(AdminDto? admin)
    {
        if (admin is null) return;
        if (ConfirmDelete != null && !ConfirmDelete(admin.Name)) return;
        try
        {
            await _api.DeleteAdminAsync(admin.Id);
            await LoadAsync();
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }
}
