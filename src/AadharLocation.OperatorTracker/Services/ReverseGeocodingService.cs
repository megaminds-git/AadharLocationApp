using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace AadharLocation.OperatorTracker.Services;

public record LocationDetails(string Area, string City, string State);

public interface IReverseGeocodingService
{
    Task<LocationDetails> GetDetailsAsync(double lat, double lon, CancellationToken ct = default);
}

public class ReverseGeocodingService(IHttpClientFactory httpFactory, ILogger<ReverseGeocodingService> logger) : IReverseGeocodingService
{
    private static readonly LocationDetails Unknown = new("—", "—", "—");

    public async Task<LocationDetails> GetDetailsAsync(double lat, double lon, CancellationToken ct = default)
    {
        try
        {
            var http = httpFactory.CreateClient("Nominatim");
            var result = await http.GetFromJsonAsync<NominatimResponse>(
                $"reverse?lat={lat:F6}&lon={lon:F6}&format=json", ct);

            var a = result?.Address;
            if (a is null) return Unknown;

            return new LocationDetails(
                Area:  a.Suburb ?? a.Neighbourhood ?? a.Quarter ?? a.CityDistrict ?? a.Residential ?? a.Road ?? "—",
                City:  a.City   ?? a.Town ?? a.Village ?? a.Municipality ?? a.StateDistrict ?? "—",
                State: a.State  ?? "—");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Reverse geocoding failed for {Lat},{Lon}", lat, lon);
            return Unknown;
        }
    }

    private record NominatimResponse(
        [property: JsonPropertyName("address")] NominatimAddress? Address);

    private record NominatimAddress(
        [property: JsonPropertyName("suburb")]         string? Suburb,
        [property: JsonPropertyName("neighbourhood")]  string? Neighbourhood,
        [property: JsonPropertyName("quarter")]        string? Quarter,
        [property: JsonPropertyName("city_district")]  string? CityDistrict,
        [property: JsonPropertyName("residential")]    string? Residential,
        [property: JsonPropertyName("road")]           string? Road,
        [property: JsonPropertyName("city")]           string? City,
        [property: JsonPropertyName("town")]           string? Town,
        [property: JsonPropertyName("village")]        string? Village,
        [property: JsonPropertyName("municipality")]   string? Municipality,
        [property: JsonPropertyName("state_district")] string? StateDistrict,
        [property: JsonPropertyName("state")]          string? State);
}
