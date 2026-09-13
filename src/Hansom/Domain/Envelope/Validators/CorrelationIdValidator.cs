using FluentValidation;
using Hansom.Domain.Envelope.ValueObjects;

namespace Hansom.Domain.Envelope.Validators;

public sealed class CorrelationIdValidator : AbstractValidator<CorrelationId>
{
    public CorrelationIdValidator()
    {
        RuleFor(x => x.Value)
            .NotEmpty()
            .WithMessage("CorrelationId must not be an empty Guid.");
    }
}
