using AadharLocation.Api.Data;
using AadharLocation.Api.Domain.Entities;
using AadharLocation.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AadharLocation.Api.Features.Settings;

[ApiController]
[Route("api/settings")]
[Authorize(Roles = "Admin")]
public class SettingsController(EmailService emailService, AppDbContext db) : ControllerBase
{
    private const string RecipientsKey = "Email:AlertEmailRecipients";

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var recipientsSetting = await db.AppSettings.FindAsync(RecipientsKey);
        return Ok(new Dictionary<string, string>
        {
            ["AlertEmailRecipients"] = recipientsSetting?.Value ?? string.Empty,
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
        if (!settings.TryGetValue("AlertEmailRecipients", out var recipientsRaw))
            return BadRequest(new { message = "AlertEmailRecipients is required." });

        var value = string.Join(",", recipientsRaw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase));

        var existing = await db.AppSettings.FindAsync(RecipientsKey);
        if (existing is null)
            db.AppSettings.Add(new AppSetting { Key = RecipientsKey, Value = value });
        else
            existing.Value = value;

        await db.SaveChangesAsync();
        return Ok();
    }
}
