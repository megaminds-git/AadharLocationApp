using System.Windows.Controls;
using AadharLocation.AdminDashboard.ViewModels;

namespace AadharLocation.AdminDashboard.Views.Pages;

public partial class AlertsPage : UserControl
{
    private readonly AlertsViewModel _vm;

    public AlertsPage(AlertsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
    }

    public async Task ActivateAsync() => await _vm.LoadAsync();
}
