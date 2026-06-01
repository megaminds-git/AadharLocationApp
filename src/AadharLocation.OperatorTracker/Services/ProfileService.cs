using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AadharLocation.Shared.Constants;
using AadharLocation.Shared.DTOs.Auth;
using Microsoft.Extensions.Http;

namespace AadharLocation.OperatorTracker.Services;

public interface IProfileService
{
    Task<TrackerProfileResponse?> GetProfileAsync(CancellationToken ct = default);
}

public class ProfileService(IHttpClientFactory httpClientFactory, IActivationService activation) : IProfileService
{
    public async Task<TrackerProfileResponse?> GetProfileAsync(CancellationToken ct = default)
    {
        var creds = activation.GetCredentials();
        if (creds is null) return null;

        var http = httpClientFactory.CreateClient("Api");
        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", creds.Token);

        var response = await http.GetAsync(ApiRoutes.Auth.TrackerProfile, ct);
        if (!response.IsSuccessStatusCode) return null;

        return await response.Content.ReadFromJsonAsync<TrackerProfileResponse>(ct);
    }
}
