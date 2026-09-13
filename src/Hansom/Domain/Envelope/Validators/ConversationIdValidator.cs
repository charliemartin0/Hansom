using FluentValidation;
using Hansom.Domain.Envelope.ValueObjects;

namespace Hansom.Domain.Envelope.Validators;

public sealed class ConversationIdValidator : AbstractValidator<ConversationId>
{
    public ConversationIdValidator()
    {
        RuleFor(x => x.Value)
            .NotEmpty()
            .WithMessage("ConversationId must not be an empty Guid.");
    }
}
