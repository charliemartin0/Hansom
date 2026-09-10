using MiniVerine.Domain.Envelope;

namespace MiniVerine.Application.Persistence;

/// <summary>
/// Short-lived outbox transaction, one per handler call. Implemented by the in-memory store;
/// the Postgres adapter wraps a real Npgsql connection so outbox rows commit with the
/// handler's writes. A single instance is single-use: once committed or rolled back, any
/// further call throws <see cref="InvalidOperationException"/>.
/// </summary>
public interface IOutboxTransaction
{
    /// <summary>
    /// Stage a cascaded envelope in this transaction. Throws once the transaction completed.
    /// </summary>
    ValueTask StageAsync(Envelope envelope, CancellationToken ct = default);

    /// <summary>
    /// Commit staged envelopes as pending and complete the transaction.
    /// </summary>
    ValueTask CommitAsync(CancellationToken ct = default);

    /// <summary>
    /// Discard staged envelopes and complete the transaction.
    /// </summary>
    ValueTask RollbackAsync(CancellationToken ct = default);
}
