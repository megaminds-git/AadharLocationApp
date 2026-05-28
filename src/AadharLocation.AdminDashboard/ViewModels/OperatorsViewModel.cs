using System.Collections.ObjectModel;
using AadharLocation.AdminDashboard.Infrastructure;
using AadharLocation.Shared.DTOs.Operators;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AadharLocation.AdminDashboard.ViewModels;

public partial class OperatorsViewModel : ObservableObject
{
    private readonly ApiClient _api;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private OperatorDto? _selectedOperator;

    public ObservableCollection<OperatorDto> Operators { get; } = [];

    public event Action<OperatorDto?>? EditRequested;
    public event Action? AddRequested;

    private const int PageSize = 20;

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
        try
        {
            await _api.DeleteOperatorAsync(target.Id);
            await LoadAsync();
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }

    [RelayCommand]
    private async Task SearchAsync() { CurrentPage = 1; await LoadAsync(); }

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
}
