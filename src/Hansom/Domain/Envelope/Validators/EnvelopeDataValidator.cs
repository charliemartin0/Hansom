using FluentValidation;
using Hansom.Domain.Envelope.ValueObjects;

namespace Hansom.Domain.Envelope.Validators;

public sealed class EnvelopeDataValidator : AbstractValidator<EnvelopeData>
{
    public EnvelopeDataValidator()
    {
        // Empty is valid until Serialization has run. Pairing with ContentType is on EnvelopeValidator.
    }
}
