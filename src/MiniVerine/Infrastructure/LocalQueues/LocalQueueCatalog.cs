using System.Collections.Concurrent;
using MiniVerine.Application.Bus;
using MiniVerine.Application.Discovery;
using MiniVerine.Application.Execution;
using MiniVerine.Domain.Envelope;
using MiniVerine.Domain.Envelope.ValueObjects;

namespace MiniVerine.Infrastructure.LocalQueues;

/// <summary>
/// Destination → local queue agent. One agent per destination URI, created lazily.
/// Also the <see cref="IPublishEnqueuer"/> the Mediator publishes through.
/// </summary>
public sealed class LocalQueueCatalog : IPublishEnqueuer
{
    private readonly ConcurrentDictionary<string, LocalQueueAgent> _agents = new(StringComparer.Ordinal);
    private readonly HandlerCatalog _catalog;
    private readonly Executor _executor;

    public LocalQueueCatalog(HandlerCatalog catalog, Executor? executor = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        _catalog = catalog;
        _executor = executor ?? new Executor(new ErrorPolicyCatalog());
    }

    public IEnumerable<LocalQueueAgent> All => _agents.Values;

    public LocalQueueAgent For(Destination destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        string key = destination.Value.ToString();
        return _agents.GetOrAdd(key, _ => new LocalQueueAgent(key, _catalog, _executor));
    }

    public void Enqueue(Envelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        For(envelope.Destination).Enqueue(envelope);
    }
}
