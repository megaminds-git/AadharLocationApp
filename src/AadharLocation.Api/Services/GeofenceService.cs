using AadharLocation.Api.Data;
using AadharLocation.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AadharLocation.Api.Services;

public class GeofenceService(AppDbContext db, AlertService alertService)
{
    private const double EarthRadius = 6_371_000;

    public async Task<bool> CheckGeofenceAsync(Machine machine, Operator op, double lat, double lon)
    {
        var fence = await db.Geofences
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.MachineId == machine.Id && g.IsActive);

        if (fence == null) return true;

        var distance = HaversineDistance(lat, lon, fence.CenterLatitude, fence.CenterLongitude);
        var isWithin = distance <= fence.RadiusMeters;

        if (!isWithin)
        {
            var excess = distance - fence.RadiusMeters;
            await alertService.CreateGeofenceBreachAlertAsync(machine, op, lat, lon, excess);
        }

        return isWithin;
    }

    private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return EarthRadius * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRadians(double deg) => deg * Math.PI / 180;
}
