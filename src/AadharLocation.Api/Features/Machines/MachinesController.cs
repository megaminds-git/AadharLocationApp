using System.Text;
using AadharLocation.Api.Data;
using AadharLocation.Api.Domain.Entities;
using AadharLocation.Shared.DTOs;
using AadharLocation.Shared.DTOs.Machines;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AadharLocation.Api.Features.Machines;

[ApiController]
[Route("api/machines")]
[Authorize(Roles = "Admin")]
public class MachinesController(AppDbContext db) : ControllerBase
{
    private static readonly TimeZoneInfo IstZone = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "India Standard Time" : "Asia/Kolkata");

    [HttpGet("export")]
    public async Task<IActionResult> Export()
    {
        var machines = await db.Machines
            .Include(m => m.AssignedOperator)
            .Include(m => m.Geofences.Where(g => g.IsActive))
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Name,Machine Code,Assigned Operator,Status,Boundary,Last Seen");

        foreach (var m in machines)
        {
            var name     = m.Name.Replace("\"", "\"\"");
            var code     = m.SerialNumber.Replace("\"", "\"\"");
            var op       = (m.AssignedOperator?.Name ?? string.Empty).Replace("\"", "\"\"");
            var lastSeen = m.LastSeenAt.HasValue
                ? TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(m.LastSeenAt.Value, DateTimeKind.Utc), IstZone)
                    .ToString("yyyy-MM-dd HH:mm:ss")
                : string.Empty;

            var fence = m.Geofences?.FirstOrDefault(g => g.IsActive);
            string boundary;
            if (fence == null || !m.CurrentLatitude.HasValue || !m.CurrentLongitude.HasValue)
                boundary = "N/A";
            else
            {
                var dist = HaversineDistance(
                    m.CurrentLatitude.Value, m.CurrentLongitude.Value,
                    fence.CenterLatitude, fence.CenterLongitude);
                boundary = dist <= fence.RadiusMeters ? "Inside" : "Outside";
            }

            sb.AppendLine($"\"{name}\",\"{code}\",\"{op}\",\"{m.Status}\",\"{boundary}\",\"{lastSeen}\"");
        }

        var fileName = $"machines_{DateTime.Now:yyyyMMddHHmmss}.csv";
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        var query = db.Machines
            .Include(m => m.AssignedOperator)
            .Include(m => m.Geofences.Where(g => g.IsActive))
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(m =>
                m.Name.ToLower().Contains(s) ||
                m.SerialNumber.ToLower().Contains(s) ||
                (m.AssignedOperator != null && m.AssignedOperator.Name.ToLower().Contains(s)));
        }

        var total = await query.CountAsync();
        var machines = await query
            .OrderBy(m => m.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PagedResult<MachineDto>(machines.Select(ToDto).ToList(), total, page, pageSize));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var machine = await db.Machines
            .Include(m => m.AssignedOperator)
            .Include(m => m.Geofences.Where(g => g.IsActive))
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (machine is null) return NotFound();
        return Ok(ToDto(machine));
    }

    [HttpGet("live")]
    [Authorize]
    public async Task<IActionResult> GetLive()
    {
        var machines = await db.Machines
            .Include(m => m.AssignedOperator)
            .Include(m => m.Geofences.Where(g => g.IsActive))
            .AsNoTracking()
            .ToListAsync();

        return Ok(machines.Select(ToDto).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMachineRequest request)
    {
        if (await db.Machines.AnyAsync(m => m.SerialNumber == request.SerialNumber))
            return Conflict(new { message = $"Machine code '{request.SerialNumber}' is already in use." });

        if (request.AssignedOperatorId.HasValue &&
            await db.Machines.AnyAsync(m => m.AssignedOperatorId == request.AssignedOperatorId))
            return Conflict(new { message = "This operator is already assigned to another machine." });

        var machine = new Machine
        {
            Name = request.Name,
            SerialNumber = request.SerialNumber,
            Type = string.Empty,
            AssignedOperatorId = request.AssignedOperatorId,
            MachineAuthCode = Guid.NewGuid().ToString("N")[..8].ToUpper(),
        };

        db.Machines.Add(machine);
        await db.SaveChangesAsync();

        if (request.AssignedOperatorId.HasValue)
        {
            var op = await db.Operators.FindAsync(request.AssignedOperatorId.Value);
            if (op != null) op.AssignedMachineId = machine.Id;
            await db.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(GetById), new { id = machine.Id }, ToDto(machine));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMachineRequest request)
    {
        var machine = await db.Machines.FindAsync(id);
        if (machine is null) return NotFound();

        if (request.AssignedOperatorId.HasValue &&
            await db.Machines.AnyAsync(m => m.AssignedOperatorId == request.AssignedOperatorId && m.Id != id))
            return Conflict(new { message = "This operator is already assigned to another machine." });

        // Sync Operator.AssignedMachineId on both sides
        if (machine.AssignedOperatorId != request.AssignedOperatorId)
        {
            if (machine.AssignedOperatorId.HasValue)
            {
                var oldOp = await db.Operators.FindAsync(machine.AssignedOperatorId.Value);
                if (oldOp != null) oldOp.AssignedMachineId = null;
            }
            if (request.AssignedOperatorId.HasValue)
            {
                var newOp = await db.Operators.FindAsync(request.AssignedOperatorId.Value);
                if (newOp != null) newOp.AssignedMachineId = id;
            }
        }

        machine.Name = request.Name;
        machine.AssignedOperatorId = request.AssignedOperatorId;
        machine.Status = request.Status;
        machine.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var machine = await db.Machines.FindAsync(id);
        if (machine is null) return NotFound();

        machine.IsDeleted = true;
        machine.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static MachineDto ToDto(Machine m)
    {
        var fence = m.Geofences?.FirstOrDefault(g => g.IsActive);
        bool? isWithinGeofence = null;
        if (fence != null && m.CurrentLatitude.HasValue && m.CurrentLongitude.HasValue)
        {
            var dist = HaversineDistance(
                m.CurrentLatitude.Value, m.CurrentLongitude.Value,
                fence.CenterLatitude, fence.CenterLongitude);
            isWithinGeofence = dist <= fence.RadiusMeters;
        }
        return new(m.Id, m.Name, m.SerialNumber, m.Type,
            m.AssignedOperatorId, m.AssignedOperator?.Name,
            m.CurrentLatitude, m.CurrentLongitude, m.LastSeenAt,
            m.Status, m.CreatedAt, m.UpdatedAt, m.MachineAuthCode, m.IsDeleted,
            isWithinGeofence, m.CurrentLocationType);
    }

    private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double r = 6_371_000;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return r * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}
