using AadharLocation.Shared.Enums;

namespace AadharLocation.Api.Domain.Entities;

public class Machine
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int? AssignedOperatorId { get; set; }
    public Operator? AssignedOperator { get; set; }
    public double? CurrentLatitude { get; set; }
    public double? CurrentLongitude { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public MachineStatus Status { get; set; } = MachineStatus.Offline;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Geofence> Geofences { get; set; } = [];
    public ICollection<LocationLog> LocationLogs { get; set; } = [];
    public ICollection<Alert> Alerts { get; set; } = [];
    public ICollection<TrackerActivation> TrackerActivations { get; set; } = [];
}
