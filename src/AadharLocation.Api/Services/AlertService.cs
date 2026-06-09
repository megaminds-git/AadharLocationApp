using AadharLocation.Api.Data;
using AadharLocation.Api.Domain.Entities;
using AadharLocation.Api.Hubs;
using AadharLocation.Shared.DTOs.SignalR;
using AadharLocation.Shared.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace AadharLocation.Api.Services;

public class AlertService(
    AppDbContext db,
    IHubContext<AadharLocationHub, ITrackingClient> hub,
    IOptions<GeofenceSettings> settings,
    EmailService emailService,
    ILogger<AlertService> logger)
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

        var adminEmails = await GetAdminEmailsAsync();
        FireAndForgetEmail(emailService.SendGeofenceBreachAlertAsync(
            machine.Name, op.Name, lat, lon, distanceMeters, alert.CreatedAt, adminEmails));

        return alert;
    }

    public async Task<Alert?> CreateGeofenceEntryAlertAsync(
        Machine machine, Operator op, double lat, double lon)
    {
        var lastBreach = await db.Alerts
            .Where(a => a.MachineId == machine.Id && a.AlertType == AlertType.GeofenceBreach)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => (DateTime?)a.CreatedAt)
            .FirstOrDefaultAsync();

        if (lastBreach == null) return null;

        var alreadyRecorded = await db.Alerts.AnyAsync(a =>
            a.MachineId == machine.Id &&
            a.AlertType == AlertType.GeofenceEntry &&
            a.CreatedAt > lastBreach);

        if (alreadyRecorded) return null;

        var alert = new Alert
        {
            MachineId = machine.Id,
            OperatorId = op.Id,
            AlertType = AlertType.GeofenceEntry,
            Message = $"Machine '{machine.Name}' returned inside geofence boundary.",
            Latitude = lat,
            Longitude = lon,
            CreatedAt = DateTime.UtcNow
        };

        db.Alerts.Add(alert);
        await db.SaveChangesAsync();

        await hub.Clients.Group("admins").OperatorEventAlertReceived(new OperatorEventAlert(
            alert.Id, machine.Id, machine.Name, op.Id, op.Name,
            AlertType.GeofenceEntry, alert.CreatedAt));

        var adminEmails = await GetAdminEmailsAsync();
        FireAndForgetEmail(emailService.SendGeofenceEntryAlertAsync(
            machine.Name, op.Name, lat, lon, alert.CreatedAt, adminEmails));

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

        var adminEmails = await GetAdminEmailsAsync();
        FireAndForgetEmail(emailService.SendMachineOfflineAlertAsync(
            machine.Name, lastSeenAt, minutesOffline, adminEmails));

        return alert;
    }

    public async Task<Alert> CreateLogoutAlertAsync(int machineId, int operatorId)
    {
        var machine = await db.Machines.FindAsync(machineId);
        var op = await db.Operators.FindAsync(operatorId);

        var alert = new Alert
        {
            MachineId = machineId,
            OperatorId = operatorId,
            AlertType = AlertType.Logout,
            Message = $"Operator '{op?.Name ?? operatorId.ToString()}' logged out from machine '{machine?.Name ?? machineId.ToString()}'.",
            CreatedAt = DateTime.UtcNow
        };

        db.Alerts.Add(alert);
        await db.SaveChangesAsync();

        await hub.Clients.Group("admins").OperatorEventAlertReceived(new OperatorEventAlert(
            alert.Id, machineId, machine?.Name ?? string.Empty,
            operatorId, op?.Name ?? string.Empty,
            AlertType.Logout, alert.CreatedAt));

        return alert;
    }

    public async Task<Alert> CreateUninstallAlertAsync(int machineId, int operatorId)
    {
        var machine = await db.Machines.FindAsync(machineId);
        var op = await db.Operators.FindAsync(operatorId);

        var alert = new Alert
        {
            MachineId = machineId,
            OperatorId = operatorId,
            AlertType = AlertType.Uninstall,
            Message = $"Operator '{op?.Name ?? operatorId.ToString()}' uninstalled the tracker from machine '{machine?.Name ?? machineId.ToString()}'.",
            CreatedAt = DateTime.UtcNow
        };

        db.Alerts.Add(alert);
        await db.SaveChangesAsync();

        await hub.Clients.Group("admins").OperatorEventAlertReceived(new OperatorEventAlert(
            alert.Id, machineId, machine?.Name ?? string.Empty,
            operatorId, op?.Name ?? string.Empty,
            AlertType.Uninstall, alert.CreatedAt));

        return alert;
    }

    private void FireAndForgetEmail(Task emailTask) =>
        emailTask.ContinueWith(t =>
        {
            if (t.IsFaulted && t.Exception is { } ex)
                logger.LogError(ex, "Background email send failed");
        }, TaskContinuationOptions.OnlyOnFaulted);

    private async Task<List<string>> GetAdminEmailsAsync()
    {
        var adminEmails = await db.Users
            .Where(u => u.Role == "Admin" && u.Email != string.Empty)
            .Select(u => u.Email)
            .ToListAsync();

        var setting = await db.AppSettings.FindAsync("Email:AlertEmailRecipients");
        var extraEmails = setting?.Value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? [];

        return adminEmails.Union(extraEmails, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
