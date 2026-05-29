using AadharLocation.Api.Data;
using AadharLocation.Api.Domain.Entities;
using AadharLocation.Api.Hubs;
using AadharLocation.Shared.DTOs.SignalR;
using AadharLocation.Shared.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AadharLocation.Api.Services;

public class AlertService(
    AppDbContext db,
    IHubContext<AadharLocationHub, ITrackingClient> hub,
    IOptions<GeofenceSettings> settings,
    EmailService emailService)
{
    public async Task<Alert?> CreateGeofenceBreachAlertAsync(
        Machine machine, Operator op, double lat, double lon, double distanceMeters)
    {
        var cooldown = settings.Value.BreachCooldownMinutes;
        var since = DateTime.UtcNow.AddMinutes(-cooldown);

        var recentBreach = await db.Alerts.AnyAsync(a =>
            a.MachineId == machine.Id &&
            a.AlertType == AlertType.GeofenceBreach &&
            a.CreatedAt >= since);

        if (recentBreach) return null;

        var alert = new Alert
        {
            MachineId = machine.Id,
            OperatorId = op.Id,
            AlertType = AlertType.GeofenceBreach,
            Message = $"Machine '{machine.Name}' exited geofence. Distance: {distanceMeters:F0}m outside boundary.",
            Latitude = lat,
            Longitude = lon,
            CreatedAt = DateTime.UtcNow
        };

        db.Alerts.Add(alert);
        await db.SaveChangesAsync();

        await hub.Clients.Group("admins").GeofenceBreachDetected(new GeofenceBreachEvent(
            alert.Id, machine.Id, machine.Name, op.Id, op.Name,
            lat, lon, distanceMeters, alert.CreatedAt));

        _ = emailService.SendGeofenceBreachAlertAsync(
            machine.Name, op.Name, lat, lon, distanceMeters, alert.CreatedAt);

        return alert;
    }

    public async Task<Alert?> CreateOfflineAlertAsync(Machine machine)
    {
        var cooldown = settings.Value.OfflineAlertCooldownMinutes;
        var since = DateTime.UtcNow.AddMinutes(-cooldown);

        var recentOffline = await db.Alerts.AnyAsync(a =>
            a.MachineId == machine.Id &&
            a.AlertType == AlertType.Offline &&
            a.CreatedAt >= since);

        if (recentOffline) return null;

        int? operatorId = machine.AssignedOperatorId;
        if (operatorId == null)
        {
            var lastLog = await db.LocationLogs
                .Where(l => l.MachineId == machine.Id)
                .OrderByDescending(l => l.RecordedAt)
                .FirstOrDefaultAsync();
            operatorId = lastLog?.OperatorId;
        }

        if (operatorId == null) return null;

        var lastSeenAt = machine.LastSeenAt ?? DateTime.UtcNow;
        var minutesOffline = (int)(DateTime.UtcNow - lastSeenAt).TotalMinutes;

        var alert = new Alert
        {
            MachineId = machine.Id,
            OperatorId = operatorId.Value,
            AlertType = AlertType.Offline,
            Message = $"Machine '{machine.Name}' has been offline for {minutesOffline} minutes.",
            CreatedAt = DateTime.UtcNow
        };

        db.Alerts.Add(alert);
        await db.SaveChangesAsync();

        _ = emailService.SendMachineOfflineAlertAsync(machine.Name, lastSeenAt, minutesOffline);

        return alert;
    }
}
