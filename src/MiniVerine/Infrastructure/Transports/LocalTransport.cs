using MiniVerine.Application.Bus;
using MiniVerine.Application.Discovery;
using MiniVerine.Application.Execution;
using MiniVerine.Application.Transports;
using MiniVerine.Domain.Envelope;

namespace MiniVerine.Infrastructure.Transports;

/// <summary>
/// The local:// transport. <see cref="SendAsync"/> hands envelopes to
/// <see cref="IPublishEnqueuer"/> (LocalQueueCatalog by default) so outbound
/// envelopes join the same per-destination agent that Mediator.PublishAsync uses.
/// <see cref="DeliverAsync"/> runs inbound envelopes straight through Execution —
/// there is no wire to read for local://, so the listen path is synchronous on
/// the caller's thread. Mirrors <c>LocalQueueAgent.ProcessAsync</c> for the listen
/// side but skips the channel/worker.
/// </summary>
public sealed class LocalTransport : ITransport
{
    private readonly IPublishEnqueuer _enqueuer;
    private readonly HandlerCatalog _handlers;
    private readonly Executor _executor;

    public LocalTransport(IPublishEnqueuer enqueuer, HandlerCatalog handlers, Executor executor)
    {
        ArgumentNullException.ThrowIfNull(enqueuer);
        ArgumentNullException.ThrowIfNull(handlers);
        ArgumentNullException.ThrowIfNull(executor);
        _enqueuer = enqueuer;
        _handlers = handlers;
        _executor = executor;
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
        HandlerLookup lookup = _handlers.Lookup(envelope.Message.Value.GetType());
        if (lookup is MissingHandler)
        {
            await _executor.HandleMissingAsync(envelope, cancellationToken);
            return;
        }

        foreach (DiscoveredHandler handler in ((FoundHandlers)lookup).Handlers)
        {
            await _executor.InvokeAsync(envelope, handler with { Scheduled = true }, cancellationToken);
        }
    }
}
