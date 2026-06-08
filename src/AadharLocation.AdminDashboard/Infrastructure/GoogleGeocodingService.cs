using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace AadharLocation.AdminDashboard.Infrastructure;

public interface IGeocodingService
{
    Task<string> GetAddressAsync(double lat, double lon);
    Task<string> GetCityAsync(double lat, double lon);
    Task<(double Lat, double Lon)?> SearchPlaceAsync(string query);
}

public class GoogleGeocodingService : IGeocodingService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public GoogleGeocodingService(HttpClient http, IOptions<GoogleMapsOptions> options)
    {
        _http   = http;
        _apiKey = options.Value.ApiKey;
    }

    public async Task<string> GetAddressAsync(double lat, double lon)
    {
        try
        {
            var url    = $"json?latlng={lat:F6},{lon:F6}&key={_apiKey}";
            var result = await _http.GetFromJsonAsync<GoogleGeocodeResponse>(url);
            if (result?.Status != "OK" || result.Results.Length == 0) return string.Empty;
            return result.Results[0].FormattedAddress;
        }
        catch { return string.Empty; }
    }

    public async Task<string> GetCityAsync(double lat, double lon)
    {
        try
        {
            var url    = $"json?latlng={lat:F6},{lon:F6}&key={_apiKey}";
            var result = await _http.GetFromJsonAsync<GoogleGeocodeResponse>(url);
            if (result?.Status != "OK" || result.Results.Length == 0) return string.Empty;
            return ExtractCity(result.Results[0].AddressComponents);
        }
        catch { return string.Empty; }
    }

    public async Task<(double Lat, double Lon)?> SearchPlaceAsync(string query)
    {
        try
        {
            var url    = $"json?address={Uri.EscapeDataString(query)}&key={_apiKey}";
            var result = await _http.GetFromJsonAsync<GoogleGeocodeResponse>(url);
            if (result?.Status != "OK" || result.Results.Length == 0) return null;
            var loc = result.Results[0].Geometry.Location;
            return (loc.Lat, loc.Lng);
        }
        catch { return null; }
    }

    private static string ExtractCity(AddressComponent[] components)
    {
        var priority = new[] { "locality", "administrative_area_level_2", "administrative_area_level_1" };
        foreach (var type in priority)
        {
            var match = components.FirstOrDefault(c => c.Types.Contains(type));
            if (match is not null) return match.LongName;
        }
        return string.Empty;
    }
}

// ── Response models ───────────────────────────────────────────────────────────

record GoogleGeocodeResponse(
    [property: JsonPropertyName("status")]  string Status,
    [property: JsonPropertyName("results")] GoogleGeocodeResult[] Results);

record GoogleGeocodeResult(
    [property: JsonPropertyName("formatted_address")]  string FormattedAddress,
    [property: JsonPropertyName("address_components")] AddressComponent[] AddressComponents,
    [property: JsonPropertyName("geometry")]           GeocodeGeometry Geometry);

record GeocodeGeometry(
    [property: JsonPropertyName("location")] LatLng Location);

record LatLng(
    [property: JsonPropertyName("lat")] double Lat,
    [property: JsonPropertyName("lng")] double Lng);

record AddressComponent(
    [property: JsonPropertyName("long_name")] string LongName,
    [property: JsonPropertyName("types")]     string[] Types);
