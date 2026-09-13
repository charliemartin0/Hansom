using FluentValidation;
using Hansom.Domain.Errors.ValueObjects;

namespace Hansom.Domain.Errors.Validators;

public sealed class RetryWithCooldownValidator : AbstractValidator<RetryWithCooldown>
{
    public RetryWithCooldownValidator()
    {
        RuleFor(x => x.Delay)
            .GreaterThanOrEqualTo(TimeSpan.Zero)
            .WithMessage("RetryWithCooldown delay must not be negative.");
    }
}
