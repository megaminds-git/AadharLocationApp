using AadharLocation.Shared.DTOs.Machines;
using FluentValidation;

namespace AadharLocation.Api.Features.Machines.Validators;

public class CreateMachineRequestValidator : AbstractValidator<CreateMachineRequest>
{
    public CreateMachineRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SerialNumber).NotEmpty().MaximumLength(100);
    }
}
