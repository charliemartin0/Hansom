using Npgsql;

namespace Hansom.Postgresql;

/// <summary>
/// Bootstrap helpers for the Postgres adapter. Tables are not auto-migrated on construction;
/// the caller invokes the relevant <c>Ensure*Async</c> method once at startup, typically from
/// the DI registration. Each helper is idempotent (<c>CREATE ... IF NOT EXISTS</c>) so re-running
/// it is safe.
/// </summary>
public static class PostgresSchema
{
    /// <summary>
    /// Create the <c>hansom_outbox</c> table and its pending-row index if they do not already exist.
    /// <para>
    /// Shape: <c>(id UUID PK, session_id UUID, status TEXT CHECK IN ('staged','pending'),
    /// envelope JSONB, created_at TIMESTAMPTZ DEFAULT now())</c>. The session id isolates staged rows
    /// per stage/commit/rollback cycle; the partial index keeps <c>LoadPendingAsync</c> scans cheap.
    /// </para>
    /// </summary>
    public static async ValueTask EnsureOutboxTableAsync(NpgsqlDataSource dataSource, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dataSource);

        const string sql = """
            CREATE TABLE IF NOT EXISTS hansom_outbox (
                id UUID PRIMARY KEY,
                session_id UUID NOT NULL,
                status TEXT NOT NULL CHECK (status IN ('staged', 'pending')),
                envelope JSONB NOT NULL,
                created_at TIMESTAMPTZ NOT NULL DEFAULT now()
            );

            CREATE INDEX IF NOT EXISTS hansom_outbox_pending_idx
                ON hansom_outbox (created_at)
                WHERE status = 'pending';
            """;

        await using NpgsqlConnection conn = await dataSource.OpenConnectionAsync(ct);
        await using NpgsqlCommand cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
