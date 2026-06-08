using System.Text;
using AadharLocation.Api.Data;
using AadharLocation.Api.Services;
using AadharLocation.Shared.DTOs;
using AadharLocation.Shared.DTOs.Alerts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AadharLocation.Api.Features.Reports;

public record EmailAlertReportRequest(
    string Email, int[]? MachineIds, int[]? OperatorIds,
    DateTime? From, DateTime? To);

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin")]
public class ReportsController(AppDbContext db, EmailService emailService, ILogger<ReportsController> logger) : ControllerBase
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
        sb.AppendLine("Machine Name,Machine Code,Operator,Latitude,Longitude,Accuracy (m),Recorded At,Within Geofence");

        foreach (var log in logs)
        {
            var machineName  = log.MachineName.Replace("\"", "\"\"");
            var machineCode  = log.MachineCode.Replace("\"", "\"\"");
            var operatorName = log.OperatorName.Replace("\"", "\"\"");

            sb.AppendLine(
                $"\"{machineName}\",\"{machineCode}\",\"{operatorName}\"," +
                $"{log.Latitude},{log.Longitude},{log.Accuracy}," +
                $"\"{log.RecordedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}\",{log.IsWithinGeofence}");
        }

        var fileName = $"location_report_{DateTime.Now:yyyyMMddHHmmss}.csv";
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
            })
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Alert Type,Machine Name,Operator Name,Message,Latitude,Longitude,Created At");

        foreach (var a in alerts)
        {
            var machineName  = a.MachineName.Replace("\"",  "\"\"");
            var operatorName = a.OperatorName.Replace("\"", "\"\"");
            var message      = a.Message.Replace("\"",      "\"\"");

            sb.AppendLine(
                $"\"{a.AlertType}\",\"{machineName}\",\"{operatorName}\",\"{message}\"," +
                $"{(a.Latitude.HasValue  ? a.Latitude.Value.ToString()  : string.Empty)}," +
                $"{(a.Longitude.HasValue ? a.Longitude.Value.ToString() : string.Empty)}," +
                $"\"{a.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}\"");
        }

        var fileName = $"alert_report_{DateTime.Now:yyyyMMddHHmmss}.csv";
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", fileName);
    }

    [HttpPost("alerts/email")]
    public async Task<IActionResult> EmailAlerts([FromBody] EmailAlertReportRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { message = "Email address is required." });

        var query = db.Alerts
            .Include(a => a.Machine)
            .Include(a => a.Operator)
            .AsNoTracking();

        if (req.MachineIds?.Length  > 0) query = query.Where(a => req.MachineIds.Contains(a.MachineId));
        if (req.OperatorIds?.Length > 0) query = query.Where(a => req.OperatorIds.Contains(a.OperatorId));
        if (req.From.HasValue) query = query.Where(a => a.CreatedAt >= req.From.Value.ToUniversalTime());
        if (req.To.HasValue)   query = query.Where(a => a.CreatedAt <= req.To.Value.ToUniversalTime());

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
            })
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Alert Type,Machine Name,Operator Name,Message,Latitude,Longitude,Created At");

        foreach (var a in alerts)
        {
            var machineName  = a.MachineName.Replace("\"",  "\"\"");
            var operatorName = a.OperatorName.Replace("\"", "\"\"");
            var message      = a.Message.Replace("\"",      "\"\"");

            sb.AppendLine(
                $"\"{a.AlertType}\",\"{machineName}\",\"{operatorName}\",\"{message}\"," +
                $"{(a.Latitude.HasValue  ? a.Latitude.Value.ToString()  : string.Empty)}," +
                $"{(a.Longitude.HasValue ? a.Longitude.Value.ToString() : string.Empty)}," +
                $"\"{a.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}\"");
        }

        var fileName = $"alert_report_{DateTime.Now:yyyyMMddHHmmss}.csv";
        var csvBytes = Encoding.UTF8.GetBytes(sb.ToString());

        try
        {
            await emailService.SendAlertReportAsync(csvBytes, fileName, req.Email);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Resend error while emailing report to {Email}", req.Email);
            return Problem($"Failed to send email: {ex.Message}");
        }

        return Ok(new { message = $"Report sent to {req.Email}" });
    }
}
