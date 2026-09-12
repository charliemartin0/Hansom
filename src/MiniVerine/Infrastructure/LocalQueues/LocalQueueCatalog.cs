using System.Collections.Concurrent;
using MiniVerine.Application.Bus;
using MiniVerine.Domain.Envelope;
using MiniVerine.Domain.Envelope.ValueObjects;

namespace MiniVerine.Infrastructure.LocalQueues;

/// <summary>
/// Destination → local queue agent. One agent per destination URI, created lazily.
/// Also the <see cref="IPublishEnqueuer"/> the Mediator publishes through. Agents share
/// the single <see cref="MessageDelivery"/> so every queue runs the same Executor.
/// </summary>
public sealed class LocalQueueCatalog : IPublishEnqueuer
{
    private readonly ConcurrentDictionary<string, LocalQueueAgent> _agents = new(StringComparer.Ordinal);
    private readonly MessageDelivery _delivery;

    public LocalQueueCatalog(MessageDelivery delivery)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        _delivery = delivery;
    }

    public IEnumerable<LocalQueueAgent> All => _agents.Values;

    public LocalQueueAgent For(Destination destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        string key = destination.Value.ToString();
        return _agents.GetOrAdd(key, _ => new LocalQueueAgent(key, _delivery));
    }

    public void Enqueue(Envelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        For(envelope.Destination).Enqueue(envelope);
    }
}