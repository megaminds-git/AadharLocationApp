using AadharLocation.Api.Data;
using AadharLocation.Api.Hubs;
using AadharLocation.Shared.DTOs;
using AadharLocation.Shared.DTOs.Alerts;
using AadharLocation.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AadharLocation.Api.Features.Alerts;

[ApiController]
[Route("api/alerts")]
[Authorize(Roles = "Admin")]
public class AlertsController(
    AppDbContext db,
    IHubContext<AadharLocationHub, ITrackingClient> hub) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? machineId,
        [FromQuery] AlertType? type,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = db.Alerts
            .Include(a => a.Machine)
            .Include(a => a.Operator)
            .AsNoTracking();

        if (machineId.HasValue) query = query.Where(a => a.MachineId == machineId.Value);
        if (type.HasValue) query = query.Where(a => a.AlertType == type.Value);
        if (from.HasValue) query = query.Where(a => a.CreatedAt >= from.Value.ToUniversalTime());
        if (to.HasValue) query = query.Where(a => a.CreatedAt <= to.Value.ToUniversalTime());

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => ToDto(a))
            .ToListAsync();

        return Ok(new PagedResult<AlertDto>(items, total, page, pageSize));
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var count = await db.Alerts.CountAsync(a => !a.IsAcknowledged);
        return Ok(new AlertSummaryDto(count));
    }

    [HttpPut("{id:int}/acknowledge")]
    public async Task<IActionResult> Acknowledge(int id)
    {
        var alert = await db.Alerts.FindAsync(id);
        if (alert is null) return NotFound();

        alert.IsAcknowledged = true;
        alert.AcknowledgedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await hub.Clients.Group("admins").AlertAcknowledged(id);
        return NoContent();
    }

    private static AlertDto ToDto(Domain.Entities.Alert a) => new(
        a.Id, a.MachineId, a.Machine.Name, a.OperatorId, a.Operator.Name,
        a.AlertType, a.Message, a.Latitude, a.Longitude,
        a.CreatedAt, a.IsAcknowledged, a.AcknowledgedAt);
}
