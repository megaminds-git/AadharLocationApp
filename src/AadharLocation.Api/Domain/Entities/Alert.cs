using AadharLocation.Shared.Enums;

namespace AadharLocation.Api.Domain.Entities;

public class Alert
{
    public int Id { get; set; }
    public int MachineId { get; set; }
    public Machine Machine { get; set; } = null!;
    public int OperatorId { get; set; }
    public Operator Operator { get; set; } = null!;
    public AlertType AlertType { get; set; }
    public string Message { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsAcknowledged { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
}
