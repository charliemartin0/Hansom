using Hansom.Application.Persistence;
using Hansom.Domain.Envelope;
using Hansom.Postgresql;
using Npgsql;

namespace Hansom.Tests.Postgresql;

/// <summary>
/// Prove-with for the Postgres <see cref="IOutboxTransaction"/>. Staged envelopes are
/// buffered in memory and become pending rows only on commit; rollback leaves nothing; and
/// the single-use completion contract the kernel middleware relies on holds on a real
/// Npgsql connection.
/// </summary>
public sealed class PostgresOutboxTransactionTests : IClassFixture<PostgresContainerFixture>, IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private PostgresOutboxStore _store = null!;

    public PostgresOutboxTransactionTests(PostgresContainerFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetOutboxAsync();
        _store = new PostgresOutboxStore(_fixture.DataSource);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task commit_writes_pending_rows_visible_via_load_pending()
    {
        IOutboxTransaction transaction = _store.BeginOutboxTransaction();
        Envelope envelope = PostgresOutboxTestEnvelope.Create();

        await transaction.StageAsync(envelope);
        await transaction.CommitAsync();

        Envelope pending = Assert.Single(await _store.LoadPendingAsync());
        Assert.Equal(envelope.Id, pending.Id);
        await ((IAsyncDisposable)transaction).DisposeAsync();
    }

    [Fact]
    public async Task rollback_writes_no_rows()
    {
        IOutboxTransaction transaction = _store.BeginOutboxTransaction();
        await transaction.StageAsync(PostgresOutboxTestEnvelope.Create());

        await transaction.RollbackAsync();

        Assert.Empty(await _store.LoadPendingAsync());
        await ((IAsyncDisposable)transaction).DisposeAsync();
    }

    [Fact]
    public async Task stage_does_not_persist_until_commit()
    {
        IOutboxTransaction transaction = _store.BeginOutboxTransaction();

        await transaction.StageAsync(PostgresOutboxTestEnvelope.Create());

        Assert.Empty(await _store.LoadPendingAsync());
        await ((IAsyncDisposable)transaction).DisposeAsync();
    }

    [Fact]
    public async Task double_commit_throws_already_completed()
    {
        IOutboxTransaction transaction = _store.BeginOutboxTransaction();
        await transaction.CommitAsync();

        await Assert.ThrowsAsync<OutboxTransactionAlreadyCompletedException>(
            async () => await transaction.CommitAsync());
        await ((IAsyncDisposable)transaction).DisposeAsync();
    }

    [Fact]
    public async Task double_rollback_throws_already_completed()
    {
        IOutboxTransaction transaction = _store.BeginOutboxTransaction();
        await transaction.RollbackAsync();

        await Assert.ThrowsAsync<OutboxTransactionAlreadyCompletedException>(
            async () => await transaction.RollbackAsync());
        await ((IAsyncDisposable)transaction).DisposeAsync();
    }

    [Fact]
    public async Task stage_after_commit_throws_already_completed()
    {
        IOutboxTransaction transaction = _store.BeginOutboxTransaction();
        await transaction.CommitAsync();

        await Assert.ThrowsAsync<OutboxTransactionAlreadyCompletedException>(
            async () => await transaction.StageAsync(PostgresOutboxTestEnvelope.Create()));
        await ((IAsyncDisposable)transaction).DisposeAsync();
    }

    [Fact]
    public async Task commit_after_rollback_throws_already_completed()
    {
        IOutboxTransaction transaction = _store.BeginOutboxTransaction();
        await transaction.RollbackAsync();

        await Assert.ThrowsAsync<OutboxTransactionAlreadyCompletedException>(
            async () => await transaction.CommitAsync());
        await ((IAsyncDisposable)transaction).DisposeAsync();
    }

    [Fact]
    public async Task commit_then_dispose_does_not_throw()
    {
        IOutboxTransaction transaction = _store.BeginOutboxTransaction();
        await transaction.StageAsync(PostgresOutboxTestEnvelope.Create());
        await transaction.CommitAsync();

        await ((IAsyncDisposable)transaction).DisposeAsync();
    }

    [Fact]
    public async Task commit_writes_rows_with_non_null_session_id()
    {
        IOutboxTransaction transaction = _store.BeginOutboxTransaction();
        Envelope envelope = PostgresOutboxTestEnvelope.Create();

        await transaction.StageAsync(envelope);
        await transaction.CommitAsync();
        await ((IAsyncDisposable)transaction).DisposeAsync();

        await using NpgsqlConnection conn = await _fixture.DataSource.OpenConnectionAsync();
        await using NpgsqlCommand cmd = new NpgsqlCommand(
            "SELECT session_id IS NOT NULL FROM hansom_outbox WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("id", envelope.Id.Value);
        object? result = await cmd.ExecuteScalarAsync();

        Assert.NotNull(result);
        Assert.True((bool)result);
    }
}
