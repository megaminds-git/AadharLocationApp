using AadharLocation.Shared.Enums;
using Microsoft.Extensions.Logging;
using Windows.Devices.Geolocation;

namespace AadharLocation.OperatorTracker.Services;

public record GpsReading(double Latitude, double Longitude, double AccuracyMeters, LocationType Source = LocationType.Unknown);

public interface IGpsService
{
    Task<GpsReading?> GetLocationAsync(CancellationToken ct = default);
}

public class GpsService : IGpsService
{
    private readonly ILogger<GpsService> _logger;
    private GeolocationAccessStatus _lastAccessStatus = GeolocationAccessStatus.Unspecified;

    public GpsService(ILogger<GpsService> logger)
    {
        _logger = logger;
    }

    public async Task<GpsReading?> GetLocationAsync(CancellationToken ct = default)
    {
        try
        {
            var accessStatus = await Geolocator.RequestAccessAsync().AsTask(ct);

            if (accessStatus == GeolocationAccessStatus.Denied)
            {
                if (_lastAccessStatus != GeolocationAccessStatus.Denied)
                    _logger.LogWarning("Location access denied — user may have revoked it after startup.");
                _lastAccessStatus = accessStatus;
                return null;
            }

            _lastAccessStatus = accessStatus;

            var geolocator = new Geolocator
            {
                DesiredAccuracy = PositionAccuracy.High,
                ReportInterval = 0
            };

            var position = await geolocator
                .GetGeopositionAsync(
                    maximumAge: TimeSpan.FromMinutes(2),
                    timeout: TimeSpan.FromSeconds(15))
                .AsTask(ct);

            var coord = position.Coordinate;
            double accuracy = coord.Accuracy;
            var source = coord.PositionSource switch
            {
                PositionSource.Satellite => LocationType.GPS,
                PositionSource.WiFi      => LocationType.WiFi,
                PositionSource.IPAddress => LocationType.IP,
                PositionSource.Cellular  => LocationType.IP,
                _                        => LocationType.Unknown
            };

            _logger.LogDebug("GPS: {Lat}, {Lon} ±{Acc}m [{Source}]", coord.Latitude, coord.Longitude, accuracy, source);
            return new GpsReading(coord.Latitude, coord.Longitude, accuracy, source);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GPS read failed — will retry next cycle.");
            return null;
        }
    }
}
