using AadharLocation.Shared.DTOs.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace AadharLocation.AdminDashboard.Infrastructure;

public class SignalRClient
{
    private HubConnection? _connection;
    private readonly AuthStateService _auth;
    private readonly string _hubUrl;
    private volatile bool _stopping;

    public event Action<MachineLocationUpdate>?  MachineLocationUpdated;
    public event Action<GeofenceBreachEvent>?    GeofenceBreachDetected;
    public event Action<MachineOfflineEvent>?    MachineWentOffline;
    public event Action<int, string>?            MachineOnline;
    public event Action<int>?                    AlertAcknowledged;
    public event Action<OperatorEventAlert>?     OperatorEventAlertReceived;
    public event Action?                         ConnectionReconnected;
    public event Action<bool>?                   ConnectionStateChanged;

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

        _stopping = false;

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

        _connection.On<OperatorEventAlert>("OperatorEventAlertReceived",
            e => OperatorEventAlertReceived?.Invoke(e));

        _connection.Reconnecting += _ => { ConnectionStateChanged?.Invoke(false); return Task.CompletedTask; };

        _connection.Reconnected += _ =>
        {
            ConnectionStateChanged?.Invoke(true);
            ConnectionReconnected?.Invoke();
            return Task.CompletedTask;
        };

        // WithAutomaticReconnect gives up after its retry list is exhausted (~60s here) and
        // raises Closed instead of retrying forever. Without this handler the connection dies
        // permanently after a network blip longer than that, with no further pushes ever.
        _connection.Closed += async _ =>
        {
            ConnectionStateChanged?.Invoke(false);
            if (!_stopping)
                await KeepReconnectingAsync();
        };

        await _connection.StartAsync();
        ConnectionStateChanged?.Invoke(true);
    }

    private async Task KeepReconnectingAsync()
    {
        var delay = TimeSpan.FromSeconds(5);
        while (!_stopping && _connection != null)
        {
            await Task.Delay(delay);
            if (_stopping || _connection == null) return;

            try
            {
                await _connection.StartAsync();
                ConnectionStateChanged?.Invoke(true);
                ConnectionReconnected?.Invoke();
                return;
            }
            catch
            {
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 60));
            }
        }
    }

    public async Task DisconnectAsync()
    {
        if (_connection != null)
            await DisposeConnectionAsync();
    }

    private async Task DisposeConnectionAsync()
    {
        if (_connection == null) return;
        _stopping = true;
        try { await _connection.StopAsync(); } catch { /* ignore */ }
        await _connection.DisposeAsync();
        _connection = null;
    }
}
