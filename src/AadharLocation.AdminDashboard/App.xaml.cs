using System.IO;
using System.Windows;
using AadharLocation.AdminDashboard.Infrastructure;
using AadharLocation.AdminDashboard.ViewModels;
using AadharLocation.AdminDashboard.Views;
using DashMainWindow = AadharLocation.AdminDashboard.Views.MainWindow;
using AadharLocation.AdminDashboard.Views.Pages;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Color = System.Windows.Media.Color;

namespace AadharLocation.AdminDashboard;

public partial class App : Application
{
    private IHost? _host;

    public static IServiceProvider Services { get; private set; } = null!;
    public static bool IsDarkTheme { get; private set; } = true;

    private static readonly string _themeFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AadharLocation", "theme.txt");

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AadharLocation");
        Directory.CreateDirectory(appDataDir);

        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(Path.Combine(appDataDir, "logs", "admin-.log"),
                rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
            .CreateLogger();

        // Load saved theme preference before any window opens
        if (File.Exists(_themeFile))
            IsDarkTheme = File.ReadAllText(_themeFile).Trim() != "light";

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(config =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((ctx, services) => ConfigureServices(ctx.Configuration, services))
            .Build();

        await _host.StartAsync();
        Services = _host.Services;

        ApplyTheme();

        var auth = Services.GetRequiredService<AuthStateService>();
        auth.LoadFromDisk();

        if (auth.IsAuthenticated)
        {
            var mainWindow = Services.GetRequiredService<DashMainWindow>();
            mainWindow.Show();
            await mainWindow.InitAsync();
        }
        else
        {
            var loginVm     = Services.GetRequiredService<LoginViewModel>();
            var loginWindow = new LoginWindow(loginVm);
            loginVm.LoginSucceeded += async () =>
            {
                loginWindow.Close();
                var main = Services.GetRequiredService<DashMainWindow>();
                main.Show();
                await main.InitAsync();
            };
            loginWindow.Show();
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private static void ConfigureServices(IConfiguration config, IServiceCollection services)
    {
        services.AddSingleton<AuthStateService>();

        services.AddHttpClient<ApiClient>(client =>
        {
            var baseUrl = config["ApiBaseUrl"] ?? "http://localhost:5000";
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddSingleton<SignalRClient>();
        services.AddSingleton<NavigationService>();

        // Page ViewModels (singleton — preserve state on navigation)
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<OperatorsViewModel>();
        services.AddSingleton<MachinesViewModel>();
        services.AddSingleton<FleetMapViewModel>();
        services.AddSingleton<AlertsViewModel>();
        services.AddSingleton<SettingsViewModel>();

        // Dialog ViewModels (transient — fresh state each dialog open)
        services.AddTransient<AddOperatorViewModel>();
        services.AddTransient<AddMachineViewModel>();
        services.AddTransient<GeofenceEditorViewModel>();

        // Main window VM and window
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<DashMainWindow>();

        // Login
        services.AddTransient<LoginViewModel>();

        // Pages (singleton — NavigationService resolves from DI)
        services.AddSingleton<DashboardPage>();
        services.AddSingleton<OperatorsPage>();
        services.AddSingleton<MachinesPage>();
        services.AddSingleton<FleetMapPage>();
        services.AddSingleton<AlertsPage>();
        services.AddSingleton<SettingsPage>();
    }

    private static void ApplyTheme() => SwitchTheme(IsDarkTheme);

    /// <summary>
    /// Switches between dark and light themes at runtime. Persists the preference to disk.
    /// </summary>
    public static void SwitchTheme(bool isDark)
    {
        IsDarkTheme = isDark;

        // Update MaterialDesign base theme
        var paletteHelper = new PaletteHelper();
        var theme = paletteHelper.GetTheme();
        theme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);
        var teal = Color.FromRgb(0x2D, 0xD4, 0xBF);
        theme.SetPrimaryColor(teal);
        theme.SetSecondaryColor(teal);
        paletteHelper.SetTheme(theme);

        // Swap custom palette dictionary
        var dicts = Current.Resources.MergedDictionaries;
        var existing = dicts.FirstOrDefault(d =>
            d.Source?.OriginalString?.EndsWith("Theme.xaml", StringComparison.OrdinalIgnoreCase) == true);
        if (existing != null) dicts.Remove(existing);

        dicts.Add(new ResourceDictionary
        {
            Source = new Uri($"/Styles/{(isDark ? "Dark" : "Light")}Theme.xaml", UriKind.Relative)
        });

        try { File.WriteAllText(_themeFile, isDark ? "dark" : "light"); }
        catch { /* ignore persistence errors */ }
    }
}
