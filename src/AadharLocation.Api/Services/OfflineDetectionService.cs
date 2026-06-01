using AadharLocation.Api.Data;
using AadharLocation.Api.Hubs;
using AadharLocation.Shared.DTOs.SignalR;
using AadharLocation.Shared.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AadharLocation.Api.Services;

public class OfflineDetectionService(
    IServiceScopeFactory scopeFactory,
    IHubContext<AadharLocationHub, ITrackingClient> hub,
    IOptions<GeofenceSettings> settings,
    ILogger<OfflineDetectionService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            await DetectOfflineMachinesAsync(stoppingToken);
        }
    }

    private async Task DetectOfflineMachinesAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var alertService = scope.ServiceProvider.GetRequiredService<AlertService>();

            var threshold = DateTime.UtcNow.AddMinutes(-settings.Value.OfflineThresholdMinutes);

            var machines = await db.Machines
                .Where(m => m.Status != MachineStatus.Offline
                         && m.LastSeenAt != null
                         && m.LastSeenAt < threshold)
                .ToListAsync(ct);

            foreach (var machine in machines)
                machine.Status = MachineStatus.Offline;

            if (machines.Count > 0)
                await db.SaveChangesAsync(ct);

            foreach (var machine in machines)
            {
                await alertService.CreateOfflineAlertAsync(machine);

                var lastSeenAt = machine.LastSeenAt ?? DateTime.UtcNow;
                var minutesOffline = (int)(DateTime.UtcNow - lastSeenAt).TotalMinutes;

                await hub.Clients.Group("admins").MachineOffline(new MachineOfflineEvent(
                    machine.Id, machine.Name, lastSeenAt, minutesOffline));

                logger.LogInformation("Machine {MachineId} ({Name}) marked offline after {Minutes} min",
                    machine.Id, machine.Name, minutesOffline);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during offline detection");
        }
    }
}
