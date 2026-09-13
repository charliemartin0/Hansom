using Hansom.Domain.Envelope;
using Hansom.Domain.Envelope.ValueObjects;
using Hansom.Domain.Messaging;
using Hansom.Domain.Messaging.ValueObjects;
using Hansom.Domain.Sagas.ValueObjects;

namespace Hansom.Tests.Domain.Envelope;

internal static class EnvelopeFactory
{
    public static Hansom.Domain.Envelope.Envelope Create(
        EnvelopeId? id = null,
        Message? message = null,
        MessageType? messageType = null,
        Destination? destination = null,
        CorrelationId? correlationId = null,
        ConversationId? conversationId = null,
        SagaId? sagaId = null,
        DateTimeOffset? sentAt = null,
        DateTimeOffset? deliverBy = null,
        Headers? headers = null,
        string contentType = "",
        int attempts = 1,
        byte[]? data = null)
    {
        var sent = sentAt ?? DateTimeOffset.UtcNow;
        return new Hansom.Domain.Envelope.Envelope(
            id ?? new EnvelopeId(Guid.NewGuid()),
            message ?? new Message(new PlaceOrder(1)),
            messageType ?? MessageTypeNaming.For(typeof(PlaceOrder)),
            destination ?? new Destination(new Uri("local://payments/")),
            correlationId ?? new CorrelationId(Guid.NewGuid()),
            conversationId ?? new ConversationId(Guid.NewGuid()),
            sagaId ?? new SagaId(""),
            new SentAt(sent),
            new DeliverBy(deliverBy),
            headers ?? new Headers(),
            new ContentType(contentType),
            new Attempts(attempts),
            data is null ? new EnvelopeData() : new EnvelopeData(data));
    }
}
