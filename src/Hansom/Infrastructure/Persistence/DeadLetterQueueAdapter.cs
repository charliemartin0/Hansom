using Hansom.Application.Execution;
using Hansom.Application.Persistence;
using Hansom.Domain.Envelope;

namespace Hansom.Infrastructure.Persistence;

/// <summary>
/// Wires the in-memory execution port to the persistence port: <see cref="IErrorQueue.Move"/>
/// records the envelope and its cause in the <see cref="IDeadLetterStore"/>. The in-memory
/// store's RecordAsync completes synchronously, so the fire-and-forget write lands before
/// the caller's next statement; a durable store adapter must revisit the void signature.
/// </summary>
internal sealed class DeadLetterQueueAdapter : IErrorQueue
{
    private readonly IDeadLetterStore _store;

    public DeadLetterQueueAdapter(IDeadLetterStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    public void Move(Envelope envelope, Exception cause)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(cause);
        _ = _store.RecordAsync(envelope, cause);
    }
}