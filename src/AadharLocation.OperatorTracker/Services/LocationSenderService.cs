using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AadharLocation.Shared.Constants;
using AadharLocation.Shared.DTOs.Location;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AadharLocation.OperatorTracker.Services;

public class LocationSenderService : BackgroundService
{
    private readonly IGpsService _gps;
    private readonly IActivationService _activation;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LocationSenderService> _logger;

    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    public event EventHandler<string>? PingSent;

    public LocationSenderService(
        IGpsService gps,
        IActivationService activation,
        IHttpClientFactory httpClientFactory,
        ILogger<LocationSenderService> logger)
    {
        _gps = gps;
        _activation = activation;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LocationSenderService started.");

        // Wait until we have credentials before starting the ping loop.
        while (!_activation.HasCredentials() && !stoppingToken.IsCancellationRequested)
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        using var timer = new PeriodicTimer(Interval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await SendPingAsync(stoppingToken);
        }
    }

    private async Task SendPingAsync(CancellationToken ct)
    {
        var creds = _activation.GetCredentials();
        if (creds is null)
        {
            _logger.LogDebug("No credentials — skipping ping.");
            return;
        }

        GpsReading? reading;
        try
        {
            reading = await _gps.GetLocationAsync(ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (reading is null)
        {
            _logger.LogWarning("GPS unavailable — skipping ping.");
            return;
        }

        var request = new LocationPingRequest(
            DeviceKey: creds.DeviceKey,
            MachineId: creds.MachineId,
            OperatorId: creds.OperatorId,
            Latitude: reading.Latitude,
            Longitude: reading.Longitude,
            Accuracy: reading.AccuracyMeters,
            RecordedAt: DateTime.UtcNow);

        try
        {
            var http = _httpClientFactory.CreateClient("Api");
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", creds.Token);

            var response = await http.PostAsJsonAsync(ApiRoutes.Location.Ping, request, ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("401 on ping — token expired, clearing credentials.");
                _activation.ClearCredentials();
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ping failed: {Status}", response.StatusCode);
                return;
            }

            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            _logger.LogInformation("Ping sent at {Time}: {Lat:F5}, {Lon:F5}", timestamp, reading.Latitude, reading.Longitude);
            PingSent?.Invoke(this, $"Last ping: {timestamp}");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ping send failed — will retry next cycle.");
        }
    }
}
