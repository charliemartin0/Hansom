using Hansom.Domain.Envelope;

namespace Hansom.Application.Persistence;

/// <summary>
/// Short-lived outbox transaction, one per handler call. Implemented by the in-memory store
/// and the Postgres adapter. The Postgres adapter wraps an Npgsql connection; the outbox's
/// own writes are atomic on that connection, while sharing it with the handler's own DB
/// writes is a follow-up slice. A single instance is single-use: once committed or rolled
/// back, any further call throws <see cref="OutboxTransactionAlreadyCompletedException"/>.
/// Staging happens on a transaction-local buffer — the long-lived store's staged set is
/// the host-replay / dispatch-recovery path and is not shared with per-handler
/// transactions, so concurrent handler attempts never clobber each other's staged rows.
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
