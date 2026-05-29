using System.Text;
using AadharLocation.Api.Data;
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
            // Escape any embedded quotes by doubling them
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
}
