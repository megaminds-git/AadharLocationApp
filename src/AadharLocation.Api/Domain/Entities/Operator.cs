using AadharLocation.Shared.Enums;

namespace AadharLocation.Api.Domain.Entities;

public class Operator
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? District { get; set; }
    public int? AssignedMachineId { get; set; }
    public Machine? AssignedMachine { get; set; }
    public string? PasswordHash { get; set; }
    public OperatorStatus Status { get; set; } = OperatorStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<LocationLog> LocationLogs { get; set; } = [];
    public ICollection<Alert> Alerts { get; set; } = [];
    public ICollection<TrackerActivation> TrackerActivations { get; set; } = [];
}
