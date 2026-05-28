using AadharLocation.Api.Domain.Entities;
using AadharLocation.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace AadharLocation.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Operator> Operators => Set<Operator>();
    public DbSet<Machine> Machines => Set<Machine>();
    public DbSet<Geofence> Geofences => Set<Geofence>();
    public DbSet<LocationLog> LocationLogs => Set<LocationLog>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<TrackerActivation> TrackerActivations => Set<TrackerActivation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Operator>(e =>
        {
            e.HasIndex(o => o.EmployeeId).IsUnique();
            e.HasOne(o => o.AssignedMachine)
             .WithMany()
             .HasForeignKey(o => o.AssignedMachineId)
             .OnDelete(DeleteBehavior.SetNull);
            e.Property(o => o.Status).HasConversion<string>();
        });

        modelBuilder.Entity<Machine>(e =>
        {
            e.HasIndex(m => m.SerialNumber).IsUnique();
            e.HasOne(m => m.AssignedOperator)
             .WithMany()
             .HasForeignKey(m => m.AssignedOperatorId)
             .OnDelete(DeleteBehavior.SetNull);
            e.Property(m => m.Status).HasConversion<string>();
        });

        modelBuilder.Entity<LocationLog>(e =>
        {
            e.HasIndex(l => new { l.MachineId, l.RecordedAt });
            e.HasOne(l => l.Machine).WithMany(m => m.LocationLogs).HasForeignKey(l => l.MachineId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.Operator).WithMany(o => o.LocationLogs).HasForeignKey(l => l.OperatorId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Alert>(e =>
        {
            e.HasIndex(a => new { a.MachineId, a.CreatedAt });
            e.HasIndex(a => a.IsAcknowledged);
            e.HasOne(a => a.Machine).WithMany(m => m.Alerts).HasForeignKey(a => a.MachineId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Operator).WithMany(o => o.Alerts).HasForeignKey(a => a.OperatorId).OnDelete(DeleteBehavior.Cascade);
            e.Property(a => a.AlertType).HasConversion<string>();
        });

        modelBuilder.Entity<TrackerActivation>(e =>
        {
            e.HasIndex(t => t.DeviceKey).IsUnique();
            e.HasOne(t => t.Operator).WithMany(o => o.TrackerActivations).HasForeignKey(t => t.OperatorId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(t => t.Machine).WithMany(m => m.TrackerActivations).HasForeignKey(t => t.MachineId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
