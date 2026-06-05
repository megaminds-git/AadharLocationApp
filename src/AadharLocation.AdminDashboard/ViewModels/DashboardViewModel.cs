using System.Windows;
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

        _signalR.MachineLocationUpdated     += OnMachineUpdate;
        _signalR.MachineWentOffline         += OnMachineOffline;
        _signalR.MachineOnline              += OnMachineOnline;
        _signalR.GeofenceBreachDetected     += OnBreach;
        _signalR.AlertAcknowledged          += OnAlertAcknowledged;
        _signalR.OperatorEventAlertReceived += OnOperatorEvent;
        _signalR.ConnectionReconnected      += () => _ = LoadAsync();
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
                TotalMachines       = machines.TotalCount;
                OnlineMachines      = machines.Items.Count(m => m.Status == MachineStatus.Online);
                OfflineMachines     = machines.Items.Count(m => m.Status == MachineStatus.Offline);
                InBoundaryMachines  = machines.Items.Count(m => m.IsWithinGeofence == true);
                OutBoundaryMachines = machines.Items.Count(m => m.IsWithinGeofence == false);
                RecentMachines      = machines.Items.Take(6).ToList();
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

    private void OnMachineUpdate(MachineLocationUpdate update)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var idx = _allMachines.FindIndex(m => m.Id == update.MachineId);
            if (idx >= 0)
                _allMachines[idx] = _allMachines[idx] with
                {
                    CurrentLatitude  = update.Latitude,
                    CurrentLongitude = update.Longitude,
                    LastSeenAt       = update.RecordedAt,
                    IsWithinGeofence = update.IsWithinGeofence
                };

            InBoundaryMachines  = _allMachines.Count(m => m.IsWithinGeofence == true);
            OutBoundaryMachines = _allMachines.Count(m => m.IsWithinGeofence == false);

            var ri = RecentMachines.FindIndex(m => m.Id == update.MachineId);
            if (ri >= 0)
            {
                var list = RecentMachines.ToList();
                list[ri] = list[ri] with
                {
                    CurrentLatitude  = update.Latitude,
                    CurrentLongitude = update.Longitude,
                    LastSeenAt       = update.RecordedAt,
                    IsWithinGeofence = update.IsWithinGeofence
                };
                RecentMachines = list;
                OnPropertyChanged(nameof(RecentMachines));
            }
        });
    }

    private void OnMachineOnline(int machineId, string machineName)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var idx = _allMachines.FindIndex(m => m.Id == machineId);
            if (idx >= 0)
                _allMachines[idx] = _allMachines[idx] with { Status = MachineStatus.Online };

            OnlineMachines  = _allMachines.Count(m => m.Status == MachineStatus.Online);
            OfflineMachines = _allMachines.Count(m => m.Status == MachineStatus.Offline);

            var ri = RecentMachines.FindIndex(m => m.Id == machineId);
            if (ri >= 0)
            {
                var list = RecentMachines.ToList();
                list[ri] = list[ri] with { Status = MachineStatus.Online };
                RecentMachines = list;
                OnPropertyChanged(nameof(RecentMachines));
            }
        });
    }

    private void OnMachineOffline(MachineOfflineEvent evt)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var idx = _allMachines.FindIndex(m => m.Id == evt.MachineId);
            if (idx >= 0)
                _allMachines[idx] = _allMachines[idx] with { Status = MachineStatus.Offline, LastSeenAt = evt.LastSeenAt };

            OnlineMachines  = _allMachines.Count(m => m.Status == MachineStatus.Online);
            OfflineMachines = _allMachines.Count(m => m.Status == MachineStatus.Offline);

            var ri = RecentMachines.FindIndex(m => m.Id == evt.MachineId);
            if (ri >= 0)
            {
                var list = RecentMachines.ToList();
                list[ri] = list[ri] with { Status = MachineStatus.Offline, LastSeenAt = evt.LastSeenAt };
                RecentMachines = list;
                OnPropertyChanged(nameof(RecentMachines));
            }
        });
    }

    private void OnBreach(GeofenceBreachEvent breach)
    {
        var newAlert = new AlertDto(
            Id:             breach.AlertId,
            MachineId:      breach.MachineId,
            MachineName:    breach.MachineName,
            OperatorId:     breach.OperatorId,
            OperatorName:   breach.OperatorName,
            AlertType:      AlertType.GeofenceBreach,
            Message:        $"Machine '{breach.MachineName}' exited geofence. Distance: {breach.DistanceMeters:F0}m outside boundary.",
            Latitude:       breach.Latitude,
            Longitude:      breach.Longitude,
            CreatedAt:      breach.OccurredAt,
            IsAcknowledged: false,
            AcknowledgedAt: null
        );

        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var idx = _allMachines.FindIndex(m => m.Id == breach.MachineId);
            if (idx >= 0)
                _allMachines[idx] = _allMachines[idx] with { IsWithinGeofence = false };

            InBoundaryMachines  = _allMachines.Count(m => m.IsWithinGeofence == true);
            OutBoundaryMachines = _allMachines.Count(m => m.IsWithinGeofence == false);

            RecentAlerts = [newAlert, .. RecentAlerts.Take(4)];
            OnPropertyChanged(nameof(RecentAlerts));

            var ri = RecentMachines.FindIndex(m => m.Id == breach.MachineId);
            if (ri >= 0)
            {
                var list = RecentMachines.ToList();
                list[ri] = list[ri] with { IsWithinGeofence = false };
                RecentMachines = list;
                OnPropertyChanged(nameof(RecentMachines));
            }
        });
    }

    private void OnAlertAcknowledged(int alertId)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            RecentAlerts = RecentAlerts.Where(a => a.Id != alertId).ToList();
            OnPropertyChanged(nameof(RecentAlerts));
        });
    }

    private void OnOperatorEvent(OperatorEventAlert evt)
    {
        var message = evt.AlertType == AlertType.Logout
            ? $"Operator '{evt.OperatorName}' logged out from machine '{evt.MachineName}'."
            : $"App was uninstalled from machine '{evt.MachineName}'.";

        var newAlert = new AlertDto(
            Id:             evt.AlertId,
            MachineId:      evt.MachineId,
            MachineName:    evt.MachineName,
            OperatorId:     evt.OperatorId,
            OperatorName:   evt.OperatorName,
            AlertType:      evt.AlertType,
            Message:        message,
            Latitude:       null,
            Longitude:      null,
            CreatedAt:      evt.CreatedAt,
            IsAcknowledged: false,
            AcknowledgedAt: null
        );

        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            RecentAlerts = [newAlert, .. RecentAlerts.Take(4)];
            OnPropertyChanged(nameof(RecentAlerts));
        });
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
