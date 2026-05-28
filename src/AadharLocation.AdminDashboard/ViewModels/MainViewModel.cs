using AadharLocation.AdminDashboard.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AadharLocation.AdminDashboard.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly NavigationService _nav;
    private readonly AuthStateService _auth;
    private readonly SignalRClient _signalR;
    private readonly AlertsViewModel _alertsVm;

    [ObservableProperty] private string _currentPageTitle = "Dashboard";
    [ObservableProperty] private NavPage _activePage = NavPage.Dashboard;
    [ObservableProperty] private int _alertBadgeCount;
    [ObservableProperty] private bool _isSignalRConnected;

    public string UserName  => _auth.UserName;
    public string UserEmail => _auth.UserEmail;

    public event Action? LogoutRequested;

    public MainViewModel(NavigationService nav, AuthStateService auth,
        SignalRClient signalR, AlertsViewModel alertsVm)
    {
        _nav       = nav;
        _auth      = auth;
        _signalR   = signalR;
        _alertsVm  = alertsVm;

        alertsVm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AlertsViewModel.UnacknowledgedCount))
                AlertBadgeCount = alertsVm.UnacknowledgedCount;
        };

        _signalR.GeofenceBreachDetected += _ => RefreshAlertBadge();
        _signalR.MachineWentOffline     += _ => RefreshAlertBadge();
    }

    public async Task InitAsync()
    {
        try
        {
            await _signalR.ConnectAsync();
            IsSignalRConnected = true;
        }
        catch { IsSignalRConnected = false; }

        var summary = await TryGetSummaryAsync();
        AlertBadgeCount = summary;
    }

    [RelayCommand]
    private void Navigate(NavPage page)
    {
        ActivePage = page;
        CurrentPageTitle = page switch
        {
            NavPage.Dashboard => "Dashboard",
            NavPage.Operators => "Operators",
            NavPage.Machines  => "Machines",
            NavPage.FleetMap  => "Fleet Map",
            NavPage.Alerts    => "Alerts",
            NavPage.Settings  => "Settings",
            _ => string.Empty
        };
        _nav.NavigateTo(page);
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _signalR.DisconnectAsync();
        _auth.ClearSession();
        LogoutRequested?.Invoke();
    }

    private async void RefreshAlertBadge() =>
        AlertBadgeCount = await TryGetSummaryAsync();

    private async Task<int> TryGetSummaryAsync()
    {
        try
        {
            var alertsVm = _alertsVm;
            var summary  = await Task.Run(async () =>
            {
                // Re-load summary via AlertsViewModel or directly
                return alertsVm.UnacknowledgedCount;
            });
            return summary;
        }
        catch { return 0; }
    }
}
