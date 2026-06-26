using AadharLocation.Api.Data;
using AadharLocation.Api.Services;
using AadharLocation.Shared.DTOs.Activation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AadharLocation.Api.Features.Activation;

[ApiController]
[Route("api/activation")]
[Authorize(Roles = "Admin")]
public class ActivationController(AppDbContext db, AlertService alertService) : ControllerBase
{
    private const string CodeChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    [HttpGet("devices")]
    public async Task<IActionResult> GetDevices()
    {
        var devices = await db.TrackerActivations
            .Include(t => t.Operator)
            .Include(t => t.Machine)
            .AsNoTracking()
            .OrderByDescending(t => t.LastPingAt)
            .Select(t => new DeviceDto(
                t.Id,
                t.DeviceKey,
                t.OperatorId,
                t.Operator.Name,
                t.MachineId,
                t.Machine.Name,
                t.LastPingAt,
                t.IsActive,
                t.UninstallCodeHash != null && t.UninstallCodeExpiry > DateTime.UtcNow))
            .ToListAsync();

        return Ok(devices);
    }

    [HttpPost("{deviceKey}/generate-uninstall-code")]
    public async Task<IActionResult> GenerateUninstallCode(string deviceKey)
    {
        var activation = await db.TrackerActivations
            .FirstOrDefaultAsync(t => t.DeviceKey == deviceKey);
        if (activation is null) return NotFound();

        var code = new string(Enumerable.Range(0, 6)
            .Select(_ => CodeChars[Random.Shared.Next(CodeChars.Length)])
            .ToArray());

        activation.UninstallCodeHash = BCrypt.Net.BCrypt.HashPassword(code);

        await db.SaveChangesAsync();
        return Ok(new GenerateUninstallCodeResponse(code));
    }

    [HttpPost("verify-uninstall-code")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyUninstallCode([FromBody] VerifyUninstallCodeRequest request)
    {
        var activation = await db.TrackerActivations
            .FirstOrDefaultAsync(t => t.DeviceKey == request.DeviceKey);
        if (activation is null) return NotFound();

        if (!activation.IsActive)
            return Ok(new { message = "Device already deactivated. Uninstall may proceed." });

        if (activation.UninstallCodeHash is null)
            return BadRequest(new { message = "No uninstall code has been set. Contact your administrator." });

        if (!BCrypt.Net.BCrypt.Verify(request.Code, activation.UninstallCodeHash))
            return BadRequest(new { message = "Incorrect uninstall code." });

        activation.IsActive = false;
        activation.UninstallCodeHash = null;
        await db.SaveChangesAsync();

        _ = alertService.CreateUninstallAlertAsync(activation.MachineId, activation.OperatorId);

        return Ok(new { message = "Device deactivated successfully." });
    }

    [HttpPost("deactivate")]
    public async Task<IActionResult> Deactivate([FromBody] DeactivateRequest request)
    {
        var activation = await db.TrackerActivations
            .FirstOrDefaultAsync(t => t.DeviceKey == request.DeviceKey);
        if (activation is null) return NotFound();

        activation.IsActive = false;
        await db.SaveChangesAsync();
        return Ok();
    }
}
