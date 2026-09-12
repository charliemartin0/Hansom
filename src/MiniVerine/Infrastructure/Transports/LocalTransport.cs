using MiniVerine.Application.Bus;
using MiniVerine.Application.Transports;
using MiniVerine.Domain.Envelope;

namespace MiniVerine.Infrastructure.Transports;

/// <summary>
/// The local:// transport. <see cref="SendAsync"/> hands envelopes to
/// <see cref="IPublishEnqueuer"/> (LocalQueueCatalog by default) so outbound
/// envelopes join the same per-destination agent that Mediator.PublishAsync uses.
/// <see cref="DeliverAsync"/> runs inbound envelopes through the shared
/// <see cref="MessageDelivery"/> — there is no wire to read for local://, so the listen
/// path is synchronous on the caller's thread. Mirrors <c>LocalQueueAgent</c> for the
/// listen side but skips the channel/worker.
/// </summary>
public sealed class LocalTransport : ITransport
{
    private readonly IPublishEnqueuer _enqueuer;
    private readonly MessageDelivery _delivery;

    public LocalTransport(IPublishEnqueuer enqueuer, MessageDelivery delivery)
    {
        ArgumentNullException.ThrowIfNull(enqueuer);
        ArgumentNullException.ThrowIfNull(delivery);
        _enqueuer = enqueuer;
        _delivery = delivery;
    }

    public string Scheme => "local";

    public ValueTask SendAsync(Envelope envelope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        _enqueuer.Enqueue(envelope);
        return ValueTask.CompletedTask;
    }

    public async ValueTask DeliverAsync(Envelope envelope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        await _delivery.Dispatch(envelope, scheduled: true, cancellationToken);
    }
}