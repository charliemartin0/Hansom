using Hansom.Domain.Envelope;
using Hansom.Domain.Envelope.ValueObjects;

namespace Hansom.Application.Persistence;

/// <summary>
/// Outbox port. Stages outgoing envelopes in the handler's transaction, commits them as
/// pending on success, and replays pending envelopes on host start. Persistence owns the
/// durable rows later; the in-memory store is the current default.
/// </summary>
public interface IOutboxStore
{
    /// <summary>
    /// Stage an outgoing envelope in the current transaction. The envelope is not yet
    /// replayable until <see cref="CommitAsync"/> succeeds.
    /// </summary>
    ValueTask StageAsync(Envelope envelope, CancellationToken ct = default);

    /// <summary>
    /// Commit staged envelopes as pending. Call after handler success.
    /// </summary>
    ValueTask CommitAsync(CancellationToken ct = default);

    /// <summary>
    /// Discard staged envelopes. Call on handler failure so a throwing handler publishes nothing.
    /// </summary>
    ValueTask RollbackAsync(CancellationToken ct = default);

    /// <summary>
    /// Load pending envelopes for replay on host start.
    /// </summary>
    ValueTask<IReadOnlyList<Envelope>> LoadPendingAsync(CancellationToken ct = default);

    /// <summary>
    /// Mark a pending envelope as sent after Execution takes ownership of it.
    /// </summary>
    ValueTask MarkSentAsync(EnvelopeId id, CancellationToken ct = default);
}
