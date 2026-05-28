using AadharLocation.Shared.DTOs.Operators;
using FluentValidation;

namespace AadharLocation.Api.Features.Operators.Validators;

public class CreateOperatorRequestValidator : AbstractValidator<CreateOperatorRequest>
{
    public CreateOperatorRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EmployeeId).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(20).When(x => x.Phone is not null);
        RuleFor(x => x.TrackerPassword)
            .MinimumLength(6)
            .When(x => x.TrackerPassword is not null)
            .WithMessage("Tracker password must be at least 6 characters.");
    }
}
