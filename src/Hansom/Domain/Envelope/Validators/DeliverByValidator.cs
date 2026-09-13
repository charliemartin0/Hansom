using FluentValidation;
using Hansom.Domain.Envelope.ValueObjects;

namespace Hansom.Domain.Envelope.Validators;

public sealed class DeliverByValidator : AbstractValidator<DeliverBy>
{
    public DeliverByValidator()
    {
        RuleFor(x => x.Value)
            .Must(value => value is null || value != default(DateTimeOffset))
            .WithMessage("DeliverBy, when set, must not be default. Comparison to SentAt is on EnvelopeValidator.");
    }
}
