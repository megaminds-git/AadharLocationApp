namespace AadharLocation.Api.Services;

public class GeofenceSettings
{
    public int BreachCooldownMinutes { get; set; } = 5;
    public int OfflineThresholdMinutes { get; set; } = 5;
    public int OfflineAlertCooldownMinutes { get; set; } = 30;
}
