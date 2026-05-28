namespace AadharLocation.Api.Domain.Entities;

public class TrackerActivation
{
    public int Id { get; set; }
    public string DeviceKey { get; set; } = string.Empty;
    public int OperatorId { get; set; }
    public Operator Operator { get; set; } = null!;
    public int MachineId { get; set; }
    public Machine Machine { get; set; } = null!;
    public DateTime? LastPingAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? UninstallCodeHash { get; set; }
    public DateTime? UninstallCodeExpiry { get; set; }
}
