using System.Windows.Controls;

namespace AadharLocation.AdminDashboard.Infrastructure;

public enum NavPage
{
    Dashboard,
    Operators,
    Machines,
    FleetMap,
    Alerts,
    Settings
}

public class NavigationService
{
    private readonly IServiceProvider _services;

    public event EventHandler<UserControl>? Navigated;
    public NavPage CurrentPage { get; private set; }

    public NavigationService(IServiceProvider services) => _services = services;

    public void NavigateTo(NavPage page)
    {
        CurrentPage = page;
        var control = GetControl(page);
        Navigated?.Invoke(this, control);
    }

    private UserControl GetControl(NavPage page) => page switch
    {
        NavPage.Dashboard  => (UserControl)_services.GetService(typeof(Views.Pages.DashboardPage))!,
        NavPage.Operators  => (UserControl)_services.GetService(typeof(Views.Pages.OperatorsPage))!,
        NavPage.Machines   => (UserControl)_services.GetService(typeof(Views.Pages.MachinesPage))!,
        NavPage.FleetMap   => (UserControl)_services.GetService(typeof(Views.Pages.FleetMapPage))!,
        NavPage.Alerts     => (UserControl)_services.GetService(typeof(Views.Pages.AlertsPage))!,
        NavPage.Settings   => (UserControl)_services.GetService(typeof(Views.Pages.SettingsPage))!,
        _ => throw new ArgumentOutOfRangeException(nameof(page))
    };
}
