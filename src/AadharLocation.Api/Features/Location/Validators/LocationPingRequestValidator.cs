using AadharLocation.Shared.DTOs.Location;
using FluentValidation;

namespace AadharLocation.Api.Features.Location.Validators;

public class LocationPingRequestValidator : AbstractValidator<LocationPingRequest>
{
    public LocationPingRequestValidator()
    {
        RuleFor(x => x.DeviceKey).NotEmpty();
        RuleFor(x => x.MachineId).GreaterThan(0);
        RuleFor(x => x.OperatorId).GreaterThan(0);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.RecordedAt).NotEmpty();
    }
}
