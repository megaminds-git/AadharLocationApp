using System.Text.Json;
using AadharLocation.Api.Data;
using AadharLocation.Api.Domain.Entities;
using AadharLocation.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AadharLocation.Api.Features.Settings;

[ApiController]
[Route("api/settings")]
[Authorize(Roles = "Admin")]
public class SettingsController(
    IOptionsMonitor<GeofenceSettings> geofenceOpts,
    EmailService emailService,
    AppDbContext db,
    IWebHostEnvironment env) : ControllerBase
{
    private const string RecipientsKey = "Email:AlertEmailRecipients";
    private string RuntimeSettingsPath =>
        Path.Combine(env.ContentRootPath, "appsettings.runtime.json");

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var geo = geofenceOpts.CurrentValue;
        var recipientsSetting = await db.AppSettings.FindAsync(RecipientsKey);
        return Ok(new Dictionary<string, string>
        {
            ["OfflineThresholdMinutes"] = geo.OfflineThresholdMinutes.ToString(),
            ["GeofenceCooldownMinutes"] = geo.BreachCooldownMinutes.ToString(),
            ["AlertEmailRecipients"]    = recipientsSetting?.Value ?? string.Empty,
        });
    }

    [HttpPost("test-email")]
    public async Task<IActionResult> TestEmail()
    {
        var adminEmails = await db.Users
            .Where(u => u.Role == "Admin" && u.Email != string.Empty)
            .Select(u => u.Email)
            .ToListAsync();

        await emailService.SendGeofenceBreachAlertAsync(
            machineName: "TEST-MACHINE-01",
            operatorName: "Test Operator",
            lat: 28.6139, lon: 77.2090,
            distanceMeters: 125.5,
            breachedAt: DateTime.UtcNow,
            adminEmails: adminEmails);
        return Ok(new { message = "Test email dispatched — check your inbox." });
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] Dictionary<string, string> settings)
    {
        // Persist email recipients to DB
        if (settings.TryGetValue("AlertEmailRecipients", out var recipientsRaw))
        {
            var normalized = recipientsRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            var value = string.Join(",", normalized);
            var existing = await db.AppSettings.FindAsync(RecipientsKey);
            if (existing is null)
                db.AppSettings.Add(new AppSetting { Key = RecipientsKey, Value = value });
            else
                existing.Value = value;

            await db.SaveChangesAsync();
        }

        // Persist geofence cooldown settings to runtime JSON (file-based, triggers reloadOnChange)
        var runtimeSettings = new
        {
            GeofenceSettings = new
            {
                BreachCooldownMinutes   = int.TryParse(settings.GetValueOrDefault("GeofenceCooldownMinutes"), out var gc) ? gc : geofenceOpts.CurrentValue.BreachCooldownMinutes,
                OfflineThresholdMinutes = int.TryParse(settings.GetValueOrDefault("OfflineThresholdMinutes"), out var ot) ? ot : geofenceOpts.CurrentValue.OfflineThresholdMinutes,
            }
        };

        var json = JsonSerializer.Serialize(runtimeSettings, new JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText(RuntimeSettingsPath, json);
        return Ok();
    }
}
