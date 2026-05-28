using AadharLocation.Shared.DTOs.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace AadharLocation.AdminDashboard.Infrastructure;

public class SignalRClient
{
    private HubConnection? _connection;
    private readonly AuthStateService _auth;
    private readonly string _hubUrl;

    public event Action<MachineLocationUpdate>?  MachineLocationUpdated;
    public event Action<GeofenceBreachEvent>?    GeofenceBreachDetected;
    public event Action<MachineOfflineEvent>?    MachineWentOffline;
    public event Action<int, string>?            MachineOnline;
    public event Action<int>?                    AlertAcknowledged;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public SignalRClient(AuthStateService auth, Microsoft.Extensions.Configuration.IConfiguration config)
    {
        _auth   = auth;
        _hubUrl = (config["ApiBaseUrl"] ?? "http://localhost:5000").TrimEnd('/') + "/hubs/tracking";
    }

    public async Task ConnectAsync()
    {
        if (_connection != null)
            await DisposeConnectionAsync();

        _connection = new HubConnectionBuilder()
            .WithUrl(_hubUrl, opts =>
            {
                opts.AccessTokenProvider = () => Task.FromResult<string?>(_auth.Token);
            })
            .WithAutomaticReconnect(new[] { TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4),
                                            TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(16),
                                            TimeSpan.FromSeconds(30) })
            .Build();

        _connection.On<MachineLocationUpdate>("MachineLocationUpdated",
            u => MachineLocationUpdated?.Invoke(u));

        _connection.On<GeofenceBreachEvent>("GeofenceBreachDetected",
            e => GeofenceBreachDetected?.Invoke(e));

        _connection.On<MachineOfflineEvent>("MachineOffline",
            e => MachineWentOffline?.Invoke(e));

        _connection.On<int, string>("MachineOnline",
            (id, name) => MachineOnline?.Invoke(id, name));

        _connection.On<int>("AlertAcknowledged",
            id => AlertAcknowledged?.Invoke(id));

        await _connection.StartAsync();
    }

    public async Task DisconnectAsync()
    {
        if (_connection != null)
            await DisposeConnectionAsync();
    }

    private async Task DisposeConnectionAsync()
    {
        if (_connection == null) return;
        try { await _connection.StopAsync(); } catch { /* ignore */ }
        await _connection.DisposeAsync();
        _connection = null;
    }
}
