using AadharLocation.Api.Data;
using AadharLocation.Api.Domain.Entities;
using AadharLocation.Api.Hubs;
using AadharLocation.Api.Services;
using AadharLocation.Shared.DTOs.Location;
using AadharLocation.Shared.DTOs.SignalR;
using AadharLocation.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AadharLocation.Api.Features.Location;

[ApiController]
[Route("api/location")]
public class LocationController(
    AppDbContext db,
    GeofenceService geofenceService,
    IHubContext<AadharLocationHub, ITrackingClient> hub) : ControllerBase
{
    [HttpPost("ping")]
    [Authorize(Policy = "TrackerOnly")]
    public async Task<IActionResult> Ping([FromBody] LocationPingRequest request)
    {
        var machine = await db.Machines
            .Include(m => m.AssignedOperator)
            .FirstOrDefaultAsync(m => m.Id == request.MachineId);
        if (machine is null) return NotFound(new { message = "Machine not found." });

        var op = await db.Operators.FindAsync(request.OperatorId);
        if (op is null) return NotFound(new { message = "Operator not found." });

        var wasOffline = machine.Status == MachineStatus.Offline;

        var isWithin = await geofenceService.CheckGeofenceAsync(machine, op, request.Latitude, request.Longitude);

        var log = new LocationLog
        {
            MachineId = machine.Id,
            OperatorId = op.Id,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Accuracy = request.Accuracy,
            RecordedAt = request.RecordedAt.ToUniversalTime(),
            IsWithinGeofence = isWithin
        };
        db.LocationLogs.Add(log);

        machine.CurrentLatitude = request.Latitude;
        machine.CurrentLongitude = request.Longitude;
        machine.LastSeenAt = DateTime.UtcNow;
        machine.Status = MachineStatus.Online;

        await db.SaveChangesAsync();

        if (wasOffline)
            await hub.Clients.Group("admins").MachineOnline(machine.Id, machine.Name);

        await hub.Clients.Group("admins").MachineLocationUpdated(new MachineLocationUpdate(
            machine.Id, machine.Name, op.Id, op.Name,
            request.Latitude, request.Longitude, request.Accuracy,
            log.RecordedAt, isWithin));

        return Ok(new LocationPingResponse(isWithin ? "ok" : "geofence_breach"));
    }
}
