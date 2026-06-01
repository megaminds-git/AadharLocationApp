using AadharLocation.OperatorTracker.Services;
using AadharLocation.OperatorTracker.ViewModels;
using AadharLocation.OperatorTracker.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.IO;
using System.Windows;
using Windows.Devices.Geolocation;
using WinForms = System.Windows.Forms;

namespace AadharLocation.OperatorTracker;

public partial class App
{
    private IHost? _host;
    private WinForms.NotifyIcon? _trayIcon;
    private ProfileWindow? _profileWindow;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(AppContext.BaseDirectory, "logs", "tracker-.log"),
                rollingInterval: RollingInterval.Day)
            .CreateLogger();

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureAppConfiguration(config =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false);
                config.AddJsonFile("server_config.json", optional: true, reloadOnChange: false);
            })
            .ConfigureServices((ctx, services) =>
            {
                var apiBase = ctx.Configuration["ApiBaseUrl"]
                    ?? throw new InvalidOperationException("ApiBaseUrl is not configured.");

                services.AddHttpClient("Api", client =>
                {
                    client.BaseAddress = new Uri(apiBase);
                    client.Timeout = TimeSpan.FromSeconds(20);
                });

                services.AddSingleton<IActivationService, ActivationService>();
                services.AddSingleton<IGpsService, GpsService>();
                services.AddSingleton<LocationSenderService>();
                services.AddHostedService(sp => sp.GetRequiredService<LocationSenderService>());

                services.AddTransient<IProfileService, ProfileService>();
                services.AddTransient<LoginViewModel>();
                services.AddTransient<LoginWindow>();
                services.AddTransient<ProfileViewModel>();
                services.AddTransient<ProfileWindow>();
                services.AddTransient<LocationAccessViewModel>();
                services.AddTransient<LocationAccessWindow>();
            })
            .Build();

        await _host.StartAsync();

        var activation = _host.Services.GetRequiredService<IActivationService>();
        var sender = _host.Services.GetRequiredService<LocationSenderService>();

        BuildTrayIcon(sender);

        activation.AuthenticationRequired += (_, _) =>
            Dispatcher.Invoke(() =>
            {
                _profileWindow?.Close();
                ShowLoginWindow();
            });

        await EnsureLocationAccessAsync();

        if (!activation.HasCredentials())
            ShowLoginWindow();
        else
            ShowProfileWindow();
    }

    private async Task EnsureLocationAccessAsync()
    {
        var status = await Geolocator.RequestAccessAsync();
        if (status != GeolocationAccessStatus.Denied)
            return;

        var tcs = new TaskCompletionSource();
        var win = _host!.Services.GetRequiredService<LocationAccessWindow>();
        win.Closed += (_, _) => tcs.TrySetResult();
        win.Show();
        await tcs.Task;
    }

    private void BuildTrayIcon(LocationSenderService sender)
    {
        _trayIcon = new WinForms.NotifyIcon
        {
            Text = "AadharLocation Tracker",
            Visible = true,
            Icon = System.Drawing.SystemIcons.Application
        };

        var menu = new WinForms.ContextMenuStrip();
        var statusItem = menu.Items.Add("Status: idle");
        statusItem!.Enabled = false;
        menu.Items.Add("-");
        menu.Items.Add("Show Profile", null, (_, _) =>
            Dispatcher.Invoke(() =>
            {
                var activation = _host!.Services.GetRequiredService<IActivationService>();
                if (activation.HasCredentials())
                    ShowProfileWindow();
            }));
        menu.Items.Add("-");
        menu.Items.Add("Exit", null, (_, _) => ExitApp());

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => Dispatcher.Invoke(() =>
        {
            var activation = _host!.Services.GetRequiredService<IActivationService>();
            if (activation.HasCredentials())
                ShowProfileWindow();
        });

        sender.PingSent += (_, msg) => Dispatcher.Invoke(() =>
        {
            _trayIcon.Text = $"AadharLocation — {msg}";
            if (menu.Items[0] is WinForms.ToolStripItem s)
                s.Text = msg;
        });
    }

    private void ShowLoginWindow()
    {
        var win = _host!.Services.GetRequiredService<LoginWindow>();
        win.Closed += (_, _) =>
        {
            var activation = _host.Services.GetRequiredService<IActivationService>();
            if (activation.HasCredentials())
                ShowProfileWindow();
            else
                ExitApp();
        };
        win.Show();
    }

    private void ShowProfileWindow()
    {
        if (_profileWindow is { IsVisible: true })
        {
            _profileWindow.Activate();
            return;
        }
        _profileWindow = _host!.Services.GetRequiredService<ProfileWindow>();
        _profileWindow.Closed += (_, _) => _profileWindow = null;
        _profileWindow.Show();
    }

    private async void ExitApp()
    {
        _trayIcon?.Dispose();
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }
        Shutdown();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
