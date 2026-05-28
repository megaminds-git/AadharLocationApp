namespace AadharLocation.Api.Domain.Entities;

public class Geofence
{
    public int Id { get; set; }
    public int MachineId { get; set; }
    public Machine Machine { get; set; } = null!;
    public double CenterLatitude { get; set; }
    public double CenterLongitude { get; set; }
    public double RadiusMeters { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
