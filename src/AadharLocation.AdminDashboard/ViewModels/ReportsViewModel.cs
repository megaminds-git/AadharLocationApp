using System.Collections.ObjectModel;
using System.IO;
using AadharLocation.AdminDashboard.Infrastructure;
using AadharLocation.Shared.DTOs.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AadharLocation.AdminDashboard.ViewModels;

public partial class ReportsViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private const int PageSize = 50;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage  = string.Empty;
    [ObservableProperty] private string _exportStatus  = string.Empty;
    [ObservableProperty] private int    _currentPage   = 1;
    [ObservableProperty] private int    _totalCount;
    [ObservableProperty] private DateTime? _fromDate = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime? _toDate   = DateTime.Today;

    public ObservableCollection<SelectableItem> MachineItems  { get; } = [];
    public ObservableCollection<SelectableItem> OperatorItems { get; } = [];
    public ObservableCollection<AlertDto>       ReportRows    { get; } = [];

    public int    TotalPages      => (int)Math.Ceiling((double)TotalCount / PageSize);
    public string TotalCountText  => TotalCount == 0
        ? string.Empty
        : $"{TotalCount} alert{(TotalCount == 1 ? "" : "s")} total";

    public string MachinesSummary  => GetSummary(MachineItems);
    public string OperatorsSummary => GetSummary(OperatorItems);

    private static string GetSummary(ObservableCollection<SelectableItem> items)
    {
        if (items.Count == 0) return "Select...";
        var selected = items.Where(i => i.IsSelected).ToList();
        if (selected.Count == 0)         return "None selected";
        if (selected.Count == items.Count) return $"All ({items.Count})";
        return selected.Count <= 2
            ? string.Join(", ", selected.Select(i => i.Label))
            : $"{selected.Count} selected";
    }

    partial void OnFromDateChanged(DateTime? value)
    {
        if (value.HasValue && ToDate.HasValue && value.Value > ToDate.Value)
            ToDate = value;
    }

    partial void OnToDateChanged(DateTime? value)
    {
        if (value.HasValue && FromDate.HasValue && value.Value < FromDate.Value)
            FromDate = value;
    }

    public ReportsViewModel(ApiClient api) => _api = api;

    [RelayCommand]
    public async Task LoadFiltersAsync()
    {
        IsLoading     = true;
        ErrorMessage  = string.Empty;
        try
        {
            var machines  = await _api.GetMachinesAsync(1, 500);
            var operators = await _api.GetOperatorsAsync(1, 500);

            bool firstLoad     = MachineItems.Count == 0 && OperatorItems.Count == 0;
            var selMachineIds  = MachineItems .Where(m => m.IsSelected).Select(m => m.Id).ToHashSet();
            var selOperatorIds = OperatorItems.Where(o => o.IsSelected).Select(o => o.Id).ToHashSet();

            MachineItems.Clear();
            if (machines?.Items != null)
                foreach (var m in machines.Items)
                {
                    var item = new SelectableItem(m.Id, m.Name)
                        { IsSelected = firstLoad || selMachineIds.Contains(m.Id) };
                    item.PropertyChanged += (_, e) =>
                        { if (e.PropertyName == nameof(SelectableItem.IsSelected)) OnPropertyChanged(nameof(MachinesSummary)); };
                    MachineItems.Add(item);
                }
            OnPropertyChanged(nameof(MachinesSummary));

            OperatorItems.Clear();
            if (operators?.Items != null)
                foreach (var o in operators.Items)
                {
                    var item = new SelectableItem(o.Id, o.Name)
                        { IsSelected = firstLoad || selOperatorIds.Contains(o.Id) };
                    item.PropertyChanged += (_, e) =>
                        { if (e.PropertyName == nameof(SelectableItem.IsSelected)) OnPropertyChanged(nameof(OperatorsSummary)); };
                    OperatorItems.Add(item);
                }
            OnPropertyChanged(nameof(OperatorsSummary));
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    public async Task ApplyAsync()
    {
        CurrentPage = 1;
        await LoadReportAsync();
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (CurrentPage * PageSize < TotalCount) { CurrentPage++; await LoadReportAsync(); }
    }

    [RelayCommand]
    private async Task PrevPageAsync()
    {
        if (CurrentPage > 1) { CurrentPage--; await LoadReportAsync(); }
    }

    private async Task LoadReportAsync()
    {
        IsLoading    = true;
        ErrorMessage = string.Empty;
        ExportStatus = string.Empty;
        try
        {
            var machineIds  = MachineItems .Where(m => m.IsSelected).Select(m => m.Id).ToArray();
            var operatorIds = OperatorItems.Where(o => o.IsSelected).Select(o => o.Id).ToArray();
            var toEndOfDay  = ToDate?.Date.AddDays(1).AddTicks(-1);

            var result = await _api.GetAlertReportAsync(
                machineIds .Length > 0 ? machineIds  : null,
                operatorIds.Length > 0 ? operatorIds : null,
                FromDate, toEndOfDay,
                CurrentPage, PageSize);

            ReportRows.Clear();
            if (result != null)
            {
                foreach (var row in result.Items) ReportRows.Add(row);
                TotalCount = result.TotalCount;
                OnPropertyChanged(nameof(TotalPages));
                OnPropertyChanged(nameof(TotalCountText));
            }
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        ExportStatus = string.Empty;
        var dialog = new SaveFileDialog
        {
            Title    = "Save Alert Report",
            Filter   = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            FileName = $"alert_report_{DateTime.Now:yyyyMMddHHmmss}.csv"
        };
        if (dialog.ShowDialog() != true) return;

        IsLoading = true;
        try
        {
            var machineIds  = MachineItems .Where(m => m.IsSelected).Select(m => m.Id).ToArray();
            var operatorIds = OperatorItems.Where(o => o.IsSelected).Select(o => o.Id).ToArray();

            var toEndOfDay = ToDate?.Date.AddDays(1).AddTicks(-1);
            var bytes = await _api.ExportAlertReportAsync(
                machineIds .Length > 0 ? machineIds  : null,
                operatorIds.Length > 0 ? operatorIds : null,
                FromDate, toEndOfDay);

            await File.WriteAllBytesAsync(dialog.FileName, bytes);
            ExportStatus = $"Saved: {Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsLoading = false; }
    }
}
