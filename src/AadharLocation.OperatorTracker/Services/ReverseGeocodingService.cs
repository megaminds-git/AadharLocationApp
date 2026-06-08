using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AadharLocation.OperatorTracker.Services;

public record LocationDetails(string Area, string City, string State);

public interface IReverseGeocodingService
{
    Task<LocationDetails> GetDetailsAsync(double lat, double lon, CancellationToken ct = default);
}

public class ReverseGeocodingService(
    IHttpClientFactory httpFactory,
    IConfiguration configuration,
    ILogger<ReverseGeocodingService> logger) : IReverseGeocodingService
{
    private static readonly LocationDetails Unknown = new("—", "—", "—");

    public async Task<LocationDetails> GetDetailsAsync(double lat, double lon, CancellationToken ct = default)
    {
        try
        {
            var apiKey = configuration["GoogleMaps:ApiKey"] ?? string.Empty;
            var http   = httpFactory.CreateClient("GoogleGeocoding");
            var result = await http.GetFromJsonAsync<GoogleGeocodeResponse>(
                $"json?latlng={lat:F6},{lon:F6}&key={apiKey}", ct);

            if (result?.Status != "OK" || result.Results.Length == 0) return Unknown;

            var components = result.Results[0].AddressComponents;
            return new LocationDetails(
                Area:  FindComponent(components, "sublocality_level_1", "sublocality", "route") ?? "—",
                City:  FindComponent(components, "locality", "administrative_area_level_2") ?? "—",
                State: FindComponent(components, "administrative_area_level_1") ?? "—");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Reverse geocoding failed for {Lat},{Lon}", lat, lon);
            return Unknown;
        }
    }

    private static string? FindComponent(AddressComponent[] components, params string[] types)
    {
        foreach (var type in types)
        {
            var match = components.FirstOrDefault(c => c.Types.Contains(type));
            if (match is not null) return match.LongName;
        }
        return null;
    }

    private record GoogleGeocodeResponse(
        [property: JsonPropertyName("status")]  string Status,
        [property: JsonPropertyName("results")] GoogleGeocodeResult[] Results);

    private record GoogleGeocodeResult(
        [property: JsonPropertyName("address_components")] AddressComponent[] AddressComponents);

    private record AddressComponent(
        [property: JsonPropertyName("long_name")] string LongName,
        [property: JsonPropertyName("types")]     string[] Types);
}
