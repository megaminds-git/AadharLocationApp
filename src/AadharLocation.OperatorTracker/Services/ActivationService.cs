using System.Net.Http;
using System.Net.Http.Json;
using AadharLocation.OperatorTracker.Infrastructure;
using AadharLocation.Shared.DTOs.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AadharLocation.OperatorTracker.Services;

public interface IActivationService
{
    bool HasCredentials();
    Task<TrackerLoginResponse> LoginAsync(string email, string password, CancellationToken ct = default);
    TrackerCredentials? GetCredentials();
    void ClearCredentials();

    event EventHandler? AuthenticationRequired;
}

public class ActivationService : IActivationService
{
    private readonly HttpClient _http;
    private readonly ILogger<ActivationService> _logger;

    private TrackerCredentials? _credentials;

    public event EventHandler? AuthenticationRequired;

    public ActivationService(IHttpClientFactory httpClientFactory, ILogger<ActivationService> logger)
    {
        _http = httpClientFactory.CreateClient("Api");
        _logger = logger;
        _credentials = AppConfig.Load();
    }

    public bool HasCredentials() => _credentials is not null;

    public TrackerCredentials? GetCredentials() => _credentials;

    public async Task<TrackerLoginResponse> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            Shared.Constants.ApiRoutes.Auth.TrackerLogin,
            new TrackerLoginRequest(email, password),
            ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Tracker login failed: {Status} {Body}", response.StatusCode, body);
            throw new InvalidOperationException("Invalid credentials. Please try again.");
        }

        var result = await response.Content.ReadFromJsonAsync<TrackerLoginResponse>(ct)
            ?? throw new InvalidOperationException("Empty response from server.");

        _credentials = new TrackerCredentials(result.Token, result.DeviceKey, result.OperatorId, result.MachineId);
        AppConfig.Save(_credentials);
        _logger.LogInformation("Tracker login successful for operator {Id}", result.OperatorId);

        return result;
    }

    public void ClearCredentials()
    {
        _credentials = null;
        AppConfig.Clear();
        _logger.LogWarning("Credentials cleared — triggering re-authentication.");
        AuthenticationRequired?.Invoke(this, EventArgs.Empty);
    }
}
