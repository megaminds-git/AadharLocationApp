using AadharLocation.Shared.DTOs.Operators;
using FluentValidation;

namespace AadharLocation.Api.Features.Operators.Validators;

public class UpdateOperatorRequestValidator : AbstractValidator<UpdateOperatorRequest>
{
    public UpdateOperatorRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(20).When(x => x.Phone is not null);
        RuleFor(x => x.NewTrackerPassword)
            .MinimumLength(6)
            .When(x => x.NewTrackerPassword is not null)
            .WithMessage("Tracker password must be at least 6 characters.");
    }
}
