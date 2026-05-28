using System.Windows.Controls;
using AadharLocation.AdminDashboard.ViewModels;

namespace AadharLocation.AdminDashboard.Views.Pages;

public partial class DashboardPage : UserControl
{
    public DashboardPage(DashboardViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    public async Task ActivateAsync() =>
        await ((DashboardViewModel)DataContext).LoadAsync();
}
