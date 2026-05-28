using AadharLocation.Shared.DTOs.Geofences;
using FluentValidation;

namespace AadharLocation.Api.Features.Geofences.Validators;

public class CreateGeofenceRequestValidator : AbstractValidator<CreateGeofenceRequest>
{
    public CreateGeofenceRequestValidator()
    {
        RuleFor(x => x.MachineId).GreaterThan(0);
        RuleFor(x => x.CenterLatitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.CenterLongitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.RadiusMeters).GreaterThan(0).LessThanOrEqualTo(100_000);
    }
}
