using System.Security.Cryptography;
using System.Text;
using AadharLocation.Api.Data;
using AadharLocation.Shared.DTOs.Activation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AadharLocation.Api.Features.Activation;

[ApiController]
[Route("api/activation")]
[Authorize(Roles = "Admin")]
public class ActivationController(AppDbContext db) : ControllerBase
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

        var keyBytes  = Encoding.UTF8.GetBytes(deviceKey);
        var codeBytes = Encoding.UTF8.GetBytes(code);
        using var hmac = new HMACSHA256(keyBytes);
        activation.UninstallCodeHash   = Convert.ToBase64String(hmac.ComputeHash(codeBytes));
        activation.UninstallCodeExpiry = DateTime.UtcNow.AddHours(24);

        await db.SaveChangesAsync();
        return Ok(new GenerateUninstallCodeResponse(code, activation.UninstallCodeExpiry.Value));
    }

    [HttpPost("verify-uninstall-code")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyUninstallCode([FromBody] VerifyUninstallCodeRequest request)
    {
        var activation = await db.TrackerActivations
            .FirstOrDefaultAsync(t => t.DeviceKey == request.DeviceKey);
        if (activation is null) return NotFound();

        if (!activation.IsActive)
            return BadRequest(new { message = "Device is already deactivated." });

        if (activation.UninstallCodeHash is null || activation.UninstallCodeExpiry < DateTime.UtcNow)
            return BadRequest(new { message = "No valid uninstall code. Please generate a new one." });

        var keyBytes  = Encoding.UTF8.GetBytes(request.DeviceKey);
        var codeBytes = Encoding.UTF8.GetBytes(request.Code.ToUpperInvariant());
        using var hmac = new HMACSHA256(keyBytes);
        var expectedHash = Convert.ToBase64String(hmac.ComputeHash(codeBytes));

        if (activation.UninstallCodeHash != expectedHash)
            return BadRequest(new { message = "Incorrect code. Contact your manager." });

        activation.IsActive             = false;
        activation.UninstallCodeHash    = null;
        activation.UninstallCodeExpiry  = null;
        await db.SaveChangesAsync();

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
