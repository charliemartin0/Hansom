using Hansom.Domain.Envelope.ValueObjects;
using Hansom.Domain.Messaging.ValueObjects;
using Hansom.Domain.Sagas.ValueObjects;

namespace Hansom.Domain.Envelope;

public record Envelope(
    EnvelopeId Id,
    Message Message,
    MessageType MessageType,
    Destination Destination,
    CorrelationId CorrelationId,
    ConversationId ConversationId,
    SagaId SagaId,
    SentAt SentAt,
    DeliverBy DeliverBy,
    Headers Headers,
    ContentType ContentType,
    Attempts Attempts,
    EnvelopeData Data);
