using System.Windows.Controls;
using AadharLocation.AdminDashboard.ViewModels;

namespace AadharLocation.AdminDashboard.Views.Pages;

public partial class ReportsPage : UserControl
{
    private readonly ReportsViewModel _vm;

    public ReportsPage(ReportsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
    }

    public async Task ActivateAsync()
    {
        await _vm.LoadFiltersAsync();
        await _vm.ApplyAsync();
    }
}
