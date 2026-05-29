using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AadharLocation.Shared.Constants;
using AadharLocation.Shared.DTOs;
using AadharLocation.Shared.DTOs.Activation;
using AadharLocation.Shared.DTOs.Alerts;
using AadharLocation.Shared.DTOs.Auth;
using AadharLocation.Shared.DTOs.Geofences;
using AadharLocation.Shared.DTOs.Machines;
using AadharLocation.Shared.DTOs.Operators;

namespace AadharLocation.AdminDashboard.Infrastructure;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly AuthStateService _auth;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public ApiClient(HttpClient http, AuthStateService auth)
    {
        _http = http;
        _auth = auth;
    }

    private void SetAuthHeader()
    {
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _auth.Token);
    }

    // ── Auth ─────────────────────────────────────────────────────────────────

    public async Task<LoginResponse?> LoginAsync(string email, string password)
    {
        var resp = await _http.PostAsJsonAsync(ApiRoutes.Auth.Login, new LoginRequest(email, password));
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<LoginResponse>(_json);
    }

    // ── Operators ─────────────────────────────────────────────────────────────

    public async Task<PagedResult<OperatorDto>?> GetOperatorsAsync(int page = 1, int pageSize = 20, string? search = null)
    {
        SetAuthHeader();
        var url = $"{ApiRoutes.Operators.Base}?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search)) url += $"&search={Uri.EscapeDataString(search)}";
        return await _http.GetFromJsonAsync<PagedResult<OperatorDto>>(url, _json);
    }

    public async Task<OperatorDto?> CreateOperatorAsync(CreateOperatorRequest req)
    {
        SetAuthHeader();
        var resp = await _http.PostAsJsonAsync(ApiRoutes.Operators.Base, req);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<OperatorDto>(_json);
    }

    public async Task UpdateOperatorAsync(int id, UpdateOperatorRequest req)
    {
        SetAuthHeader();
        var resp = await _http.PutAsJsonAsync(ApiRoutes.Operators.Base + $"/{id}", req);
        resp.EnsureSuccessStatusCode();
    }

    public async Task DeleteOperatorAsync(int id)
    {
        SetAuthHeader();
        var resp = await _http.DeleteAsync(ApiRoutes.Operators.Base + $"/{id}");
        resp.EnsureSuccessStatusCode();
    }

    // ── Machines ──────────────────────────────────────────────────────────────

    public async Task<PagedResult<MachineDto>?> GetMachinesAsync(int page = 1, int pageSize = 50)
    {
        SetAuthHeader();
        return await _http.GetFromJsonAsync<PagedResult<MachineDto>>(
            $"{ApiRoutes.Machines.Base}?page={page}&pageSize={pageSize}", _json);
    }

    public async Task<List<MachineDto>?> GetLiveMachinesAsync()
    {
        SetAuthHeader();
        return await _http.GetFromJsonAsync<List<MachineDto>>(ApiRoutes.Machines.Live, _json);
    }

    public async Task<MachineDto?> CreateMachineAsync(CreateMachineRequest req)
    {
        SetAuthHeader();
        var resp = await _http.PostAsJsonAsync(ApiRoutes.Machines.Base, req);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<MachineDto>(_json);
    }

    public async Task UpdateMachineAsync(int id, UpdateMachineRequest req)
    {
        SetAuthHeader();
        var resp = await _http.PutAsJsonAsync(ApiRoutes.Machines.Base + $"/{id}", req);
        resp.EnsureSuccessStatusCode();
    }

    public async Task DeleteMachineAsync(int id)
    {
        SetAuthHeader();
        var resp = await _http.DeleteAsync(ApiRoutes.Machines.Base + $"/{id}");
        resp.EnsureSuccessStatusCode();
    }

    // ── Geofences ─────────────────────────────────────────────────────────────

    public async Task<List<GeofenceDto>?> GetGeofencesAsync(int machineId)
    {
        SetAuthHeader();
        return await _http.GetFromJsonAsync<List<GeofenceDto>>(
            $"{ApiRoutes.Geofences.Base}?machineId={machineId}", _json);
    }

    public async Task<GeofenceDto?> CreateGeofenceAsync(CreateGeofenceRequest req)
    {
        SetAuthHeader();
        var resp = await _http.PostAsJsonAsync(ApiRoutes.Geofences.Base, req);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<GeofenceDto>(_json);
    }

    public async Task DeleteGeofenceAsync(int id)
    {
        SetAuthHeader();
        var resp = await _http.DeleteAsync(ApiRoutes.Geofences.Base + $"/{id}");
        resp.EnsureSuccessStatusCode();
    }

    // ── Alerts ────────────────────────────────────────────────────────────────

    public async Task<PagedResult<AlertDto>?> GetAlertsAsync(int page = 1, int pageSize = 30,
        bool? unackOnly = null)
    {
        SetAuthHeader();
        var url = $"{ApiRoutes.Alerts.Base}?page={page}&pageSize={pageSize}";
        if (unackOnly == true) url += "&unacknowledgedOnly=true";
        return await _http.GetFromJsonAsync<PagedResult<AlertDto>>(url, _json);
    }

    public async Task<AlertSummaryDto?> GetAlertSummaryAsync()
    {
        SetAuthHeader();
        return await _http.GetFromJsonAsync<AlertSummaryDto>(ApiRoutes.Alerts.Summary, _json);
    }

    public async Task AcknowledgeAlertAsync(int id)
    {
        SetAuthHeader();
        var resp = await _http.PutAsync(
            ApiRoutes.Alerts.Base + $"/{id}/acknowledge", null);
        resp.EnsureSuccessStatusCode();
    }

    // ── Settings ──────────────────────────────────────────────────────────────

    public async Task<Dictionary<string, string>?> GetSettingsAsync()
    {
        SetAuthHeader();
        return await _http.GetFromJsonAsync<Dictionary<string, string>>(ApiRoutes.Settings.Base, _json);
    }

    public async Task SaveSettingsAsync(Dictionary<string, string> settings)
    {
        SetAuthHeader();
        var resp = await _http.PostAsJsonAsync(ApiRoutes.Settings.Base, settings);
        resp.EnsureSuccessStatusCode();
    }

    // ── Reports ───────────────────────────────────────────────────────────────

    public async Task<byte[]> ExportMachinesCsvAsync(int? machineId, DateTime? from, DateTime? to)
    {
        SetAuthHeader();
        var parts = new List<string>();
        if (machineId.HasValue) parts.Add($"machineId={machineId.Value}");
        if (from.HasValue)      parts.Add($"from={from.Value:yyyy-MM-ddTHH:mm:ss}");
        if (to.HasValue)        parts.Add($"to={to.Value:yyyy-MM-ddTHH:mm:ss}");
        var url = parts.Count > 0
            ? ApiRoutes.Reports.Device + "?" + string.Join("&", parts)
            : ApiRoutes.Reports.Device;
        var resp = await _http.GetAsync(url);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsByteArrayAsync();
    }

    // ── Activation ────────────────────────────────────────────────────────────

    public async Task<List<DeviceDto>?> GetDevicesAsync()
    {
        SetAuthHeader();
        return await _http.GetFromJsonAsync<List<DeviceDto>>(ApiRoutes.Activation.Devices, _json);
    }

    public async Task<GenerateUninstallCodeResponse?> GenerateUninstallCodeAsync(string deviceKey)
    {
        SetAuthHeader();
        var url  = ApiRoutes.Activation.GenerateUninstallCode.Replace("{deviceKey}", Uri.EscapeDataString(deviceKey));
        var resp = await _http.PostAsync(url, null);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<GenerateUninstallCodeResponse>(_json);
    }

    public async Task DeactivateDeviceAsync(string deviceKey)
    {
        SetAuthHeader();
        var resp = await _http.PostAsJsonAsync(ApiRoutes.Activation.Deactivate, new DeactivateRequest(deviceKey));
        resp.EnsureSuccessStatusCode();
    }
}
