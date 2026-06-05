using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using AadharLocation.AdminDashboard.ViewModels;
using AadharLocation.AdminDashboard.Views.Dialogs;

namespace AadharLocation.AdminDashboard.Views.Pages;

public partial class DashboardPage : UserControl
{
    public DashboardPage(DashboardViewModel vm, IHttpClientFactory httpFactory)
    {
        InitializeComponent();
        DataContext = vm;

        vm.ShowDetail = (title, machines) =>
        {
            var dlg = new MachineStatusDetailDialog(title, machines, httpFactory) { Owner = Window.GetWindow(this) };
            dlg.ShowDialog();
        };
    }

    public async Task ActivateAsync() =>
        await ((DashboardViewModel)DataContext).LoadAsync();
}
