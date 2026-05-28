namespace AadharLocation.Api.Domain.Entities;

public class LocationLog
{
    public int Id { get; set; }
    public int MachineId { get; set; }
    public Machine Machine { get; set; } = null!;
    public int OperatorId { get; set; }
    public Operator Operator { get; set; } = null!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Accuracy { get; set; }
    public DateTime RecordedAt { get; set; }
    public bool IsWithinGeofence { get; set; }
}
