using FluentValidation;
using Hansom.Domain.Envelope.ValueObjects;

namespace Hansom.Domain.Envelope.Validators;

public sealed class AttemptsValidator : AbstractValidator<Attempts>
{
    public AttemptsValidator()
    {
        RuleFor(x => x.Value)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Attempts must be at least 1. The first execution is attempt 1.");
    }
}
