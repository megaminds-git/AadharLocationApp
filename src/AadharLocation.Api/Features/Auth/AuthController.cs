using AadharLocation.Api.Data;
using AadharLocation.Api.Domain.Entities;
using AadharLocation.Api.Services;
using AadharLocation.Shared.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AadharLocation.Api.Features.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, JwtService jwt) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        var token = jwt.GenerateAdminToken(user);
        return Ok(new LoginResponse(token, user.Name, user.Email, user.Role));
    }

    [HttpPost("tracker-login")]
    public async Task<IActionResult> TrackerLogin([FromBody] TrackerLoginRequest request)
    {
        var op = await db.Operators
            .FirstOrDefaultAsync(o => o.Email == request.Email.ToLower());

        if (op is null || op.PasswordHash is null || !BCrypt.Net.BCrypt.Verify(request.Password, op.PasswordHash))
            return Unauthorized(new { message = "Invalid credentials." });

        if (op.Status == Shared.Enums.OperatorStatus.Inactive)
            return Unauthorized(new { message = "Operator account is inactive." });

        if (op.AssignedMachineId is null)
            return BadRequest(new { message = "Operator has no assigned machine. Contact your administrator." });

        var activation = await db.TrackerActivations
            .FirstOrDefaultAsync(t => t.OperatorId == op.Id && t.IsActive);

        if (activation is null)
        {
            activation = new TrackerActivation
            {
                DeviceKey = Guid.NewGuid().ToString("N"),
                OperatorId = op.Id,
                MachineId = op.AssignedMachineId.Value,
                IsActive = true,
            };
            db.TrackerActivations.Add(activation);
            await db.SaveChangesAsync();
        }

        var token = jwt.GenerateTrackerToken(op, activation.DeviceKey);
        return Ok(new TrackerLoginResponse(token, activation.DeviceKey, op.Id, activation.MachineId));
    }
}
