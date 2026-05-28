using System.Windows;
using System.Windows.Controls;
using AadharLocation.AdminDashboard.Infrastructure;
using AadharLocation.AdminDashboard.ViewModels;
using AadharLocation.AdminDashboard.Views.Pages;
using MaterialDesignThemes.Wpf;

namespace AadharLocation.AdminDashboard.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly NavigationService _nav;
    private readonly DashboardPage _dashboardPage;
    private bool _loggingOut;

    public MainWindow(MainViewModel vm, NavigationService nav, DashboardPage dashboard)
    {
        InitializeComponent();
        _vm            = vm;
        _nav           = nav;
        _dashboardPage = dashboard;

        DataContext = vm;

        nav.Navigated      += OnNavigated;
        vm.LogoutRequested += OnLogoutRequested;

        Loaded += (_, _) => SyncThemeIcon();
        Closed += (_, _) => { if (!_loggingOut) Application.Current.Shutdown(); };
    }

    public async Task InitAsync()
    {
        await _vm.InitAsync();
        _vm.NavigateCommand.Execute(NavPage.Dashboard);
    }

    private async void OnNavigated(object? sender, UserControl page)
    {
        PageContent.Content = page;

        switch (page)
        {
            case DashboardPage p:  await p.ActivateAsync(); break;
            case OperatorsPage p:  await p.ActivateAsync(); break;
            case MachinesPage p:   await p.ActivateAsync(); break;
            case FleetMapPage p:   await p.ActivateAsync(); break;
            case AlertsPage p:     await p.ActivateAsync(); break;
            case SettingsPage p:   await p.ActivateAsync(); break;
        }
    }

    private void OnLogoutRequested()
    {
        var loginVm = (LoginViewModel)App.Services.GetService(typeof(LoginViewModel))!;
        var loginWindow = new LoginWindow(loginVm);
        loginVm.LoginSucceeded += async () =>
        {
            loginWindow.Close();
            var main = (MainWindow)App.Services.GetService(typeof(MainWindow))!;
            main.Show();
            await main.InitAsync();
        };
        loginWindow.Show();
        _loggingOut = true;
        Close();
    }

    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        App.SwitchTheme(!App.IsDarkTheme);
        SyncThemeIcon();
    }

    private void SyncThemeIcon()
    {
        // Show the opposite icon — clicking takes you to the other mode
        ThemeToggleIcon.Kind = App.IsDarkTheme
            ? PackIconKind.WeatherSunny    // dark mode active → click to go light
            : PackIconKind.WeatherNight;   // light mode active → click to go dark
    }
}
