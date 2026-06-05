using AadharLocation.AdminDashboard.Infrastructure;
using AadharLocation.Shared.DTOs.Alerts;
using AadharLocation.Shared.DTOs.Machines;
using AadharLocation.Shared.DTOs.Operators;
using AadharLocation.Shared.DTOs.SignalR;
using AadharLocation.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace AadharLocation.AdminDashboard.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly SignalRClient _signalR;
    private readonly ILogger<DashboardViewModel> _logger;

    [ObservableProperty] private int _totalMachines;
    [ObservableProperty] private int _onlineMachines;
    [ObservableProperty] private int _offlineMachines;
    [ObservableProperty] private int _totalOperators;
    [ObservableProperty] private int _inBoundaryMachines;
    [ObservableProperty] private int _outBoundaryMachines;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public List<AlertDto> RecentAlerts { get; private set; } = [];
    public List<MachineDto> RecentMachines { get; private set; } = [];

    private List<MachineDto> _allMachines = [];
    public Action<string, IEnumerable<MachineDto>>? ShowDetail { get; set; }

    public DashboardViewModel(ApiClient api, SignalRClient signalR, ILogger<DashboardViewModel> logger)
    {
        _api     = api;
        _signalR = signalR;
        _logger  = logger;

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
            var alerts    = await _api.GetAlertsAsync(pageSize: 5);

            if (machines != null)
            {
                _allMachines        = machines.Items.ToList();
                TotalMachines      = machines.TotalCount;
                OnlineMachines     = machines.Items.Count(m => m.Status == MachineStatus.Online);
                OfflineMachines    = machines.Items.Count(m => m.Status == MachineStatus.Offline);
                InBoundaryMachines  = machines.Items.Count(m => m.IsWithinGeofence == true);
                OutBoundaryMachines = machines.Items.Count(m => m.IsWithinGeofence == false);
                RecentMachines     = machines.Items.Take(6).ToList();
            }
            if (operators != null) TotalOperators = operators.TotalCount;
            if (alerts    != null) RecentAlerts   = alerts.Items.ToList();

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
            var machines = await _api.GetMachinesAsync(pageSize: 100);
            if (machines != null)
            {
                _allMachines        = machines.Items.ToList();
                OnlineMachines      = machines.Items.Count(m => m.Status == MachineStatus.Online);
                OfflineMachines     = machines.Items.Count(m => m.Status == MachineStatus.Offline);
                InBoundaryMachines  = machines.Items.Count(m => m.IsWithinGeofence == true);
                OutBoundaryMachines = machines.Items.Count(m => m.IsWithinGeofence == false);
            }
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Background dashboard refresh failed"); }
    }

    [RelayCommand]
    void ShowOnlineDetail()
    {
        var list = _allMachines.Where(m => m.Status == MachineStatus.Online).ToList();
        ShowDetail?.Invoke($"Online Machines ({list.Count})", list);
    }

    [RelayCommand]
    void ShowOfflineDetail()
    {
        var list = _allMachines.Where(m => m.Status == MachineStatus.Offline).ToList();
        ShowDetail?.Invoke($"Offline Machines ({list.Count})", list);
    }

    [RelayCommand]
    void ShowInBoundDetail()
    {
        var list = _allMachines.Where(m => m.IsWithinGeofence == true).ToList();
        ShowDetail?.Invoke($"In-Boundary Machines ({list.Count})", list);
    }

    [RelayCommand]
    void ShowOutBoundDetail()
    {
        var list = _allMachines.Where(m => m.IsWithinGeofence == false).ToList();
        ShowDetail?.Invoke($"Out-Boundary Machines ({list.Count})", list);
    }
}
