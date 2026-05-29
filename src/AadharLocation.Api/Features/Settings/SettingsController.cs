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
    IWebHostEnvironment env) : ControllerBase
{
    private string RuntimeSettingsPath =>
        Path.Combine(env.ContentRootPath, "appsettings.runtime.json");

    [HttpGet]
    public IActionResult Get()
    {
        var email   = emailOpts.CurrentValue;
        var geo     = geofenceOpts.CurrentValue;
        return Ok(new Dictionary<string, string>
        {
            ["SmtpHost"]                = email.SmtpHost,
            ["SmtpPort"]                = email.SmtpPort.ToString(),
            ["SmtpUser"]                = email.Username,
            ["FromAddress"]             = email.FromAddress,
            ["AdminRecipients"]         = string.Join(",", email.AdminRecipients),
            ["OfflineThresholdMinutes"] = geo.OfflineThresholdMinutes.ToString(),
            ["GeofenceCooldownMinutes"] = geo.BreachCooldownMinutes.ToString(),
        });
    }

    [HttpPost]
    public IActionResult Save([FromBody] Dictionary<string, string> settings)
    {
        var runtimeSettings = new
        {
            Email = new
            {
                SmtpHost   = settings.GetValueOrDefault("SmtpHost", "smtp.gmail.com"),
                SmtpPort   = int.TryParse(settings.GetValueOrDefault("SmtpPort"), out var port) ? port : 587,
                Username   = settings.GetValueOrDefault("SmtpUser", string.Empty),
                FromAddress = settings.GetValueOrDefault("FromAddress", string.Empty),
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
