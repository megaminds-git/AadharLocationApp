using System.Text.Json;
using AadharLocation.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AadharLocation.Api.Features.Settings;

[ApiController]
[Route("api/settings")]
[Authorize(Roles = "Admin")]
public class SettingsController(
    IOptionsMonitor<EmailSettings> emailOpts,
    IOptionsMonitor<GeofenceSettings> geofenceOpts,
    EmailService emailService,
    IWebHostEnvironment env) : ControllerBase
{
    private string RuntimeSettingsPath =>
        Path.Combine(env.ContentRootPath, "appsettings.runtime.json");

    [HttpGet]
    public IActionResult Get()
    {
        var email = emailOpts.CurrentValue;
        var geo   = geofenceOpts.CurrentValue;
        return Ok(new Dictionary<string, string>
        {
            ["FromAddress"]             = email.FromAddress,
            ["AdminRecipients"]         = string.Join(",", email.AdminRecipients),
            ["OfflineThresholdMinutes"] = geo.OfflineThresholdMinutes.ToString(),
            ["GeofenceCooldownMinutes"] = geo.BreachCooldownMinutes.ToString(),
        });
    }

    [HttpPost("test-email")]
    public async Task<IActionResult> TestEmail()
    {
        await emailService.SendGeofenceBreachAlertAsync(
            machineName: "TEST-MACHINE-01",
            operatorName: "Test Operator",
            lat: 28.6139, lon: 77.2090,
            distanceMeters: 125.5,
            breachedAt: DateTime.UtcNow);
        return Ok(new { message = "Test email dispatched — check your inbox." });
    }

    [HttpPost]
    public IActionResult Save([FromBody] Dictionary<string, string> settings)
    {
        var runtimeSettings = new
        {
            Email = new
            {
                FromAddress     = settings.GetValueOrDefault("FromAddress", string.Empty),
                AdminRecipients = (settings.GetValueOrDefault("AdminRecipients") ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            },
            GeofenceSettings = new
            {
                BreachCooldownMinutes   = int.TryParse(settings.GetValueOrDefault("GeofenceCooldownMinutes"), out var gc) ? gc : 5,
                OfflineThresholdMinutes = int.TryParse(settings.GetValueOrDefault("OfflineThresholdMinutes"), out var ot) ? ot : 5,
            }
        };

        var json = JsonSerializer.Serialize(runtimeSettings, new JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText(RuntimeSettingsPath, json);
        return Ok();
    }
}
