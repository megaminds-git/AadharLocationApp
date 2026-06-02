using System.Collections.ObjectModel;
using AadharLocation.AdminDashboard.Infrastructure;
using AadharLocation.Shared.DTOs.Alerts;
using AadharLocation.Shared.DTOs.SignalR;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace AadharLocation.AdminDashboard.ViewModels;

public partial class AlertsViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly SignalRClient _signalR;
    private readonly ILogger<AlertsViewModel> _logger;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private bool _showUnackOnly;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    private int _totalCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    private int _pageSize = 20;

    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));

    public ObservableCollection<AlertDto> Alerts { get; } = [];
    public int UnacknowledgedCount => Alerts.Count(a => !a.IsAcknowledged);

    public AlertsViewModel(ApiClient api, SignalRClient signalR, ILogger<AlertsViewModel> logger)
    {
        _api     = api;
        _signalR = signalR;
        _logger  = logger;
        _signalR.GeofenceBreachDetected      += _ => { var t = LoadAsync(); };
        _signalR.MachineWentOffline          += _ => { var t = LoadAsync(); };
        _signalR.OperatorEventAlertReceived  += _ => { var t = LoadAsync(); };
        _signalR.AlertAcknowledged           += OnAlertAcknowledged;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _api.GetAlertsAsync(CurrentPage, PageSize,
                ShowUnackOnly ? true : null);
            if (result != null)
            {
                Alerts.Clear();
                foreach (var a in result.Items) Alerts.Add(a);
                TotalCount = result.TotalCount;
                OnPropertyChanged(nameof(UnacknowledgedCount));
            }
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task AcknowledgeAsync(AlertDto? alert)
    {
        if (alert == null || alert.IsAcknowledged) return;
        try
        {
            await _api.AcknowledgeAlertAsync(alert.Id);
            await LoadAsync();
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }

    [RelayCommand]
    private async Task AcknowledgeAllAsync()
    {
        var unacked = Alerts.Where(a => !a.IsAcknowledged).ToList();
        foreach (var a in unacked)
        {
            try { await _api.AcknowledgeAlertAsync(a.Id); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to acknowledge alert {AlertId}", a.Id); }
        }
        await LoadAsync();
    }

    [RelayCommand]
    private async Task GoToPageAsync(int page)
    {
        if (page < 1 || page > TotalPages || page == CurrentPage) return;
        CurrentPage = page;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ChangePageSizeAsync(int size)
    {
        if (size <= 0 || size == PageSize) return;
        PageSize = size;
        CurrentPage = 1;
        await LoadAsync();
    }

    private void OnAlertAcknowledged(int id)
    {
        var alert = Alerts.FirstOrDefault(a => a.Id == id);
        if (alert == null) return;
        var idx = Alerts.IndexOf(alert);
        Alerts[idx] = alert with { IsAcknowledged = true, AcknowledgedAt = DateTime.UtcNow };
        OnPropertyChanged(nameof(UnacknowledgedCount));
    }
}
