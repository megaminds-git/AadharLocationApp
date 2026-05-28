using AadharLocation.Shared.DTOs.Machines;
using FluentValidation;

namespace AadharLocation.Api.Features.Machines.Validators;

public class UpdateMachineRequestValidator : AbstractValidator<UpdateMachineRequest>
{
    public UpdateMachineRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Type).NotEmpty().MaximumLength(100);
    }
}
