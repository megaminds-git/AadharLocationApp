using AadharLocation.AdminDashboard.Infrastructure;
using AadharLocation.Shared.DTOs.Alerts;
using AadharLocation.Shared.DTOs.Machines;
using AadharLocation.Shared.DTOs.Operators;
using AadharLocation.Shared.DTOs.SignalR;
using AadharLocation.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AadharLocation.AdminDashboard.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly SignalRClient _signalR;

    [ObservableProperty] private int _totalMachines;
    [ObservableProperty] private int _onlineMachines;
    [ObservableProperty] private int _offlineMachines;
    [ObservableProperty] private int _totalOperators;
    [ObservableProperty] private int _unacknowledgedAlerts;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public List<AlertDto> RecentAlerts { get; private set; } = [];
    public List<MachineDto> RecentMachines { get; private set; } = [];

    public DashboardViewModel(ApiClient api, SignalRClient signalR)
    {
        _api     = api;
        _signalR = signalR;

        _signalR.MachineLocationUpdated  += OnMachineUpdate;
        _signalR.MachineWentOffline      += OnMachineOffline;
        _signalR.MachineOnline           += OnMachineOnline;
        _signalR.GeofenceBreachDetected  += OnBreach;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var machines  = await _api.GetMachinesAsync(pageSize: 100);
            var operators = await _api.GetOperatorsAsync(pageSize: 100);
            var summary   = await _api.GetAlertSummaryAsync();
            var alerts    = await _api.GetAlertsAsync(pageSize: 5);

            if (machines != null)
            {
                TotalMachines   = machines.TotalCount;
                OnlineMachines  = machines.Items.Count(m => m.Status == MachineStatus.Online);
                OfflineMachines = machines.Items.Count(m => m.Status == MachineStatus.Offline);
                RecentMachines  = machines.Items.Take(6).ToList();
            }
            if (operators != null) TotalOperators = operators.TotalCount;
            if (summary  != null) UnacknowledgedAlerts = summary.UnacknowledgedCount;
            if (alerts   != null) RecentAlerts = alerts.Items.ToList();

            OnPropertyChanged(nameof(RecentAlerts));
            OnPropertyChanged(nameof(RecentMachines));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load dashboard: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    private void OnMachineUpdate(MachineLocationUpdate _) => RefreshCountsAsync();
    private void OnMachineOffline(MachineOfflineEvent _)  => RefreshCountsAsync();
    private void OnMachineOnline(int _, string __)         => RefreshCountsAsync();
    private void OnBreach(GeofenceBreachEvent _)           => RefreshCountsAsync();

    private async void RefreshCountsAsync()
    {
        try
        {
            var summary  = await _api.GetAlertSummaryAsync();
            var machines = await _api.GetMachinesAsync(pageSize: 100);
            if (summary  != null) UnacknowledgedAlerts = summary.UnacknowledgedCount;
            if (machines != null)
            {
                OnlineMachines  = machines.Items.Count(m => m.Status == MachineStatus.Online);
                OfflineMachines = machines.Items.Count(m => m.Status == MachineStatus.Offline);
            }
        }
        catch { /* silent refresh */ }
    }
}
