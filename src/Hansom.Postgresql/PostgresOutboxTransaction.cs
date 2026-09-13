using System.Text;
using System.Text.Json;
using Hansom.Application.Persistence;
using Hansom.Domain.Envelope;
using Npgsql;

namespace Hansom.Postgresql;

/// <summary>
/// Short-lived <see cref="IOutboxTransaction"/> over a real <see cref="NpgsqlConnection"/>.
/// <see cref="StageAsync"/> buffers cascaded envelopes in memory; <see cref="CommitAsync"/>
/// writes them to <c>hansom_outbox</c> as <c>pending</c> on the open connection and commits
/// the underlying transaction, so the outbox's own writes commit atomically. Single-use: any
/// call after commit or rollback throws <see cref="OutboxTransactionAlreadyCompletedException"/>.
/// <para>
/// The connection is not shared with the handler's own database writes — that is a follow-up
/// slice; today only the outbox rows are atomic on this transaction.
/// </para>
/// </summary>
internal sealed class PostgresOutboxTransaction : IOutboxTransaction, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new EnvelopeDataJsonConverter() },
    };

    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlTransaction _tx;
    private readonly List<Envelope> _staged = [];
    private readonly Guid _sessionId = Guid.NewGuid();
    private readonly object _gate = new();
    private bool _completed;

    private PostgresOutboxTransaction(NpgsqlConnection connection, NpgsqlTransaction tx)
    {
        _connection = connection;
        _tx = tx;
    }

    /// <summary>
    /// Open a connection, begin an <see cref="NpgsqlTransaction"/>, and return a single-use
    /// transaction on it. The caller owns the returned instance and must dispose it.
    /// </summary>
    public static async ValueTask<PostgresOutboxTransaction> BeginOutboxTransactionAsync(
        NpgsqlDataSource dataSource,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dataSource);

        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(ct).ConfigureAwait(false);
        try
        {
            NpgsqlTransaction tx = await connection.BeginTransactionAsync(ct).ConfigureAwait(false);
            return new PostgresOutboxTransaction(connection, tx);
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <inheritdoc />
    public ValueTask StageAsync(Envelope envelope, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        lock (_gate)
        {
            ThrowIfCompleted();
            _staged.Add(envelope);
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask CommitAsync(CancellationToken ct = default)
    {
        List<Envelope> staged;
        lock (_gate)
        {
            ThrowIfCompleted();

            // Mark complete before the I/O so a concurrent second completion throws
            // immediately. A failed insert/commit leaves the underlying transaction
            // uncommitted; disposing it (or the connection) rolls back.
            _completed = true;
            staged = [.. _staged];
            _staged.Clear();
        }

        if (staged.Count > 0)
        {
            await InsertPendingAsync(staged, ct).ConfigureAwait(false);
        }

        await _tx.CommitAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask RollbackAsync(CancellationToken ct = default)
    {
        lock (_gate)
        {
            ThrowIfCompleted();
            _completed = true;
            _staged.Clear();
        }

        await _tx.RollbackAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        bool completed;
        lock (_gate)
        {
            completed = _completed;
            _completed = true;
        }

        if (!completed)
        {
            try
            {
                await _tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // Defensive rollback — best effort. Disposing the transaction/connection
                // rolls back an uncommitted transaction anyway, so never rethrow here.
            }
        }

        await _tx.DisposeAsync().ConfigureAwait(false);
        await _connection.DisposeAsync().ConfigureAwait(false);
    }

    private async ValueTask InsertPendingAsync(IReadOnlyList<Envelope> envelopes, CancellationToken ct)
    {
        var sql = new StringBuilder(
            "INSERT INTO hansom_outbox (id, session_id, status, envelope, created_at) VALUES ");
        for (int i = 0; i < envelopes.Count; i++)
        {
            if (i > 0)
            {
                sql.Append(", ");
            }

            sql.Append("(@id").Append(i).Append(", @session_id, 'pending', @env").Append(i).Append("::jsonb, now())");
        }

        await using NpgsqlCommand cmd = new(sql.ToString(), _connection, _tx);
        cmd.Parameters.AddWithValue("session_id", _sessionId);
        for (int i = 0; i < envelopes.Count; i++)
        {
            cmd.Parameters.AddWithValue($"id{i}", envelopes[i].Id.Value);
            cmd.Parameters.AddWithValue($"env{i}", JsonSerializer.Serialize(envelopes[i], JsonOptions));
        }

        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private void ThrowIfCompleted()
    {
        if (_completed)
        {
            throw new OutboxTransactionAlreadyCompletedException();
        }
    }
}
