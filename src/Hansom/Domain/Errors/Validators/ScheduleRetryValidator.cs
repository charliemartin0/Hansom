using FluentValidation;
using Hansom.Domain.Errors.ValueObjects;

namespace Hansom.Domain.Errors.Validators;

public sealed class ScheduleRetryValidator : AbstractValidator<ScheduleRetry>
{
    public ScheduleRetryValidator()
    {
        RuleFor(x => x.Delay)
            .GreaterThan(TimeSpan.Zero)
            .WithMessage("ScheduleRetry delay must be greater than zero.");
    }
}
