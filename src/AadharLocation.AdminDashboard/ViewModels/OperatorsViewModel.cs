using System.Collections.ObjectModel;
using System.IO;
using AadharLocation.AdminDashboard.Infrastructure;
using AadharLocation.Shared.DTOs.Operators;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AadharLocation.AdminDashboard.ViewModels;

public partial class OperatorsViewModel : ObservableObject
{
    private readonly ApiClient _api;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _exportStatus = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private OperatorDto? _selectedOperator;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    private int _totalCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    private int _pageSize = 20;

    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));

    public ObservableCollection<OperatorDto> Operators { get; } = [];

    public event Action<OperatorDto?>? EditRequested;
    public event Action? AddRequested;
    public event Action<string>? UninstallCodeGenerated;
    public Func<string, bool>? ConfirmDelete { get; set; }

    public OperatorsViewModel(ApiClient api) => _api = api;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _api.GetOperatorsAsync(CurrentPage, PageSize,
                string.IsNullOrWhiteSpace(SearchText) ? null : SearchText);
            if (result != null)
            {
                Operators.Clear();
                foreach (var op in result.Items) Operators.Add(op);
                TotalCount = result.TotalCount;
            }
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void AddOperator() => AddRequested?.Invoke();

    [RelayCommand]
    private void EditOperator(OperatorDto? op) => EditRequested?.Invoke(op ?? SelectedOperator);

    [RelayCommand]
    private async Task DeleteOperatorAsync(OperatorDto? op)
    {
        var target = op ?? SelectedOperator;
        if (target == null) return;
        if (ConfirmDelete != null && !ConfirmDelete(target.Name)) return;
        try
        {
            await _api.DeleteOperatorAsync(target.Id);
            await LoadAsync();
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }

    [RelayCommand]
    private async Task GenerateUninstallCodeAsync(OperatorDto? op)
    {
        var target = op ?? SelectedOperator;
        if (target is null) return;
        try
        {
            var result = await _api.GenerateOperatorUninstallCodeAsync(target.Id);
            if (result is not null)
                UninstallCodeGenerated?.Invoke(result.Code);
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        ExportStatus = string.Empty;
        var dialog = new SaveFileDialog
        {
            Title    = "Save Operators Report",
            Filter   = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            FileName = $"operators_{DateTime.Now:yyyyMMddHHmmss}.csv"
        };
        if (dialog.ShowDialog() != true) return;

        IsLoading = true;
        try
        {
            var bytes = await _api.ExportOperatorsCsvAsync();
            await File.WriteAllBytesAsync(dialog.FileName, bytes);
            ExportStatus = $"Saved: {Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SearchAsync() { CurrentPage = 1; await LoadAsync(); }

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
}
