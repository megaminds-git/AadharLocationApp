using AadharLocation.Api.Data;
using AadharLocation.Api.Domain.Entities;
using AadharLocation.Shared.DTOs.Geofences;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AadharLocation.Api.Features.Geofences;

[ApiController]
[Route("api/geofences")]
[Authorize(Roles = "Admin")]
public class GeofencesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? machineId)
    {
        var query = db.Geofences.Include(g => g.Machine).AsNoTracking();
        if (machineId.HasValue)
            query = query.Where(g => g.MachineId == machineId.Value);

        var items = await query
            .OrderByDescending(g => g.CreatedAt)
            .Select(g => ToDto(g))
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGeofenceRequest request)
    {
        if (!await db.Machines.AnyAsync(m => m.Id == request.MachineId))
            return NotFound(new { message = "Machine not found." });

        var existing = await db.Geofences
            .Where(g => g.MachineId == request.MachineId && g.IsActive)
            .ToListAsync();
        existing.ForEach(g => g.IsActive = false);

        var geofence = new Geofence
        {
            MachineId = request.MachineId,
            CenterLatitude = request.CenterLatitude,
            CenterLongitude = request.CenterLongitude,
            RadiusMeters = request.RadiusMeters,
            IsActive = true
        };

        db.Geofences.Add(geofence);
        await db.SaveChangesAsync();

        var created = await db.Geofences
            .Include(g => g.Machine)
            .FirstAsync(g => g.Id == geofence.Id);

        return CreatedAtAction(nameof(GetAll), new { machineId = geofence.MachineId }, ToDto(created));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateGeofenceRequest request)
    {
        var geofence = await db.Geofences.FindAsync(id);
        if (geofence is null) return NotFound();

        geofence.CenterLatitude = request.CenterLatitude;
        geofence.CenterLongitude = request.CenterLongitude;
        geofence.RadiusMeters = request.RadiusMeters;

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var geofence = await db.Geofences.FindAsync(id);
        if (geofence is null) return NotFound();

        db.Geofences.Remove(geofence);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static GeofenceDto ToDto(Geofence g) => new(
        g.Id, g.MachineId, g.Machine.Name,
        g.CenterLatitude, g.CenterLongitude,
        g.RadiusMeters, g.IsActive, g.CreatedAt);
}
