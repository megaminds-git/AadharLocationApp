using System.Text;
using AadharLocation.Api.Data;
using AadharLocation.Shared.DTOs;
using AadharLocation.Shared.DTOs.Alerts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AadharLocation.Api.Features.Reports;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin")]
public class ReportsController(AppDbContext db) : ControllerBase
{
    [HttpGet("device")]
    public async Task<IActionResult> ExportDevice(
        [FromQuery] int?      machineId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var query = db.LocationLogs
            .Include(l => l.Machine)
            .Include(l => l.Operator)
            .AsNoTracking();

        if (machineId.HasValue)
            query = query.Where(l => l.MachineId == machineId.Value);
        if (from.HasValue)
            query = query.Where(l => l.RecordedAt >= from.Value.ToUniversalTime());
        if (to.HasValue)
            query = query.Where(l => l.RecordedAt <= to.Value.ToUniversalTime());

        var logs = await query
            .OrderBy(l => l.MachineId)
            .ThenBy(l => l.RecordedAt)
            .Select(l => new
            {
                MachineName  = l.Machine.Name,
                MachineCode  = l.Machine.SerialNumber,
                OperatorName = l.Operator != null ? l.Operator.Name : string.Empty,
                l.Latitude,
                l.Longitude,
                l.Accuracy,
                l.RecordedAt,
                l.IsWithinGeofence,
            })
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Machine Name,Machine Code,Operator,Latitude,Longitude,Accuracy (m),Recorded At (UTC),Within Geofence");

        foreach (var log in logs)
        {
            var machineName  = log.MachineName.Replace("\"", "\"\"");
            var machineCode  = log.MachineCode.Replace("\"", "\"\"");
            var operatorName = log.OperatorName.Replace("\"", "\"\"");

            sb.AppendLine(
                $"\"{machineName}\",\"{machineCode}\",\"{operatorName}\"," +
                $"{log.Latitude},{log.Longitude},{log.Accuracy}," +
                $"\"{log.RecordedAt:yyyy-MM-dd HH:mm:ss}\",{log.IsWithinGeofence}");
        }

        var fileName = $"location_report_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", fileName);
    }

    [HttpGet("alerts")]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] int[]?    machineIds,
        [FromQuery] int[]?    operatorIds,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int       page     = 1,
        [FromQuery] int       pageSize = 50)
    {
        var query = db.Alerts
            .Include(a => a.Machine)
            .Include(a => a.Operator)
            .AsNoTracking();

        if (machineIds?.Length  > 0) query = query.Where(a => machineIds.Contains(a.MachineId));
        if (operatorIds?.Length > 0) query = query.Where(a => operatorIds.Contains(a.OperatorId));
        if (from.HasValue) query = query.Where(a => a.CreatedAt >= from.Value.ToUniversalTime());
        if (to.HasValue)   query = query.Where(a => a.CreatedAt <= to.Value.ToUniversalTime());

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AlertDto(
                a.Id, a.MachineId, a.Machine.Name, a.OperatorId, a.Operator.Name,
                a.AlertType, a.Message, a.Latitude, a.Longitude,
                a.CreatedAt, a.IsAcknowledged, a.AcknowledgedAt))
            .ToListAsync();

        return Ok(new PagedResult<AlertDto>(items, total, page, pageSize));
    }

    [HttpGet("alerts/export")]
    public async Task<IActionResult> ExportAlerts(
        [FromQuery] int[]?    machineIds,
        [FromQuery] int[]?    operatorIds,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var query = db.Alerts
            .Include(a => a.Machine)
            .Include(a => a.Operator)
            .AsNoTracking();

        if (machineIds?.Length  > 0) query = query.Where(a => machineIds.Contains(a.MachineId));
        if (operatorIds?.Length > 0) query = query.Where(a => operatorIds.Contains(a.OperatorId));
        if (from.HasValue) query = query.Where(a => a.CreatedAt >= from.Value.ToUniversalTime());
        if (to.HasValue)   query = query.Where(a => a.CreatedAt <= to.Value.ToUniversalTime());

        var alerts = await query
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.AlertType,
                MachineName  = a.Machine.Name,
                OperatorName = a.Operator.Name,
                a.Message,
                a.Latitude,
                a.Longitude,
                a.CreatedAt,
                a.IsAcknowledged,
                a.AcknowledgedAt,
            })
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Alert Type,Machine Name,Operator Name,Message,Latitude,Longitude,Created At (UTC),Status,Acknowledged At (UTC)");

        foreach (var a in alerts)
        {
            var machineName  = a.MachineName.Replace("\"",  "\"\"");
            var operatorName = a.OperatorName.Replace("\"", "\"\"");
            var message      = a.Message.Replace("\"",      "\"\"");
            var status       = a.IsAcknowledged ? "Acknowledged" : "Pending";
            var ackedAt      = a.AcknowledgedAt.HasValue
                ? a.AcknowledgedAt.Value.ToString("yyyy-MM-dd HH:mm:ss")
                : string.Empty;

            sb.AppendLine(
                $"\"{a.AlertType}\",\"{machineName}\",\"{operatorName}\",\"{message}\"," +
                $"{(a.Latitude.HasValue  ? a.Latitude.Value.ToString()  : string.Empty)}," +
                $"{(a.Longitude.HasValue ? a.Longitude.Value.ToString() : string.Empty)}," +
                $"\"{a.CreatedAt:yyyy-MM-dd HH:mm:ss}\",\"{status}\",\"{ackedAt}\"");
        }

        var fileName = $"alert_report_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", fileName);
    }
}
