using System.Collections.ObjectModel;
using AadharLocation.AdminDashboard.Infrastructure;
using AadharLocation.Shared.DTOs.Machines;
using AadharLocation.Shared.DTOs.SignalR;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AadharLocation.AdminDashboard.ViewModels;

public partial class MachinesViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly SignalRClient _signalR;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private MachineDto? _selectedMachine;

    public ObservableCollection<MachineDto> Machines { get; } = [];

    public event Action<MachineDto?>? EditRequested;
    public event Action? AddRequested;
    public event Action<MachineDto>? GeofenceRequested;

    private const int PageSize = 20;

    public MachinesViewModel(ApiClient api, SignalRClient signalR)
    {
        _api     = api;
        _signalR = signalR;
        _signalR.MachineLocationUpdated += OnLocationUpdate;
        _signalR.MachineWentOffline     += OnMachineOffline;
        _signalR.MachineOnline          += OnMachineOnline;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _api.GetMachinesAsync(CurrentPage, PageSize);
            if (result != null)
            {
                Machines.Clear();
                foreach (var m in result.Items) Machines.Add(m);
                TotalCount = result.TotalCount;
            }
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void AddMachine() => AddRequested?.Invoke();

    [RelayCommand]
    private void SetGeofence(MachineDto? m)
    {
        var target = m ?? SelectedMachine;
        if (target != null) GeofenceRequested?.Invoke(target);
    }

    [RelayCommand]
    private void EditMachine(MachineDto? m) => EditRequested?.Invoke(m ?? SelectedMachine);

    [RelayCommand]
    private async Task DeleteMachineAsync(MachineDto? m)
    {
        var target = m ?? SelectedMachine;
        if (target == null) return;
        try { await _api.DeleteMachineAsync(target.Id); await LoadAsync(); }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (CurrentPage * PageSize < TotalCount) { CurrentPage++; await LoadAsync(); }
    }

    [RelayCommand]
    private async Task PrevPageAsync()
    {
        if (CurrentPage > 1) { CurrentPage--; await LoadAsync(); }
    }

    private void OnLocationUpdate(MachineLocationUpdate u)
    {
        var m = Machines.FirstOrDefault(x => x.Id == u.MachineId);
        if (m == null) return;
        var idx = Machines.IndexOf(m);
        Machines[idx] = m with
        {
            CurrentLatitude  = u.Latitude,
            CurrentLongitude = u.Longitude,
            LastSeenAt       = u.RecordedAt,
            Status           = Shared.Enums.MachineStatus.Online
        };
    }

    private void OnMachineOffline(MachineOfflineEvent e)
    {
        var m = Machines.FirstOrDefault(x => x.Id == e.MachineId);
        if (m == null) return;
        var idx = Machines.IndexOf(m);
        Machines[idx] = m with { Status = Shared.Enums.MachineStatus.Offline };
    }

    private void OnMachineOnline(int machineId, string _)
    {
        var m = Machines.FirstOrDefault(x => x.Id == machineId);
        if (m == null) return;
        var idx = Machines.IndexOf(m);
        Machines[idx] = m with { Status = Shared.Enums.MachineStatus.Online };
    }
}
