using MiniVerine.Application.Persistence;
using MiniVerine.Domain.Envelope;
using MiniVerine.Infrastructure.Persistence;
using MiniVerine.Tests.Domain.Envelope;

namespace MiniVerine.Tests.Infrastructure.Persistence;

public sealed class InMemoryMessageStoreDurabilityTests
{
    [Fact]
    public async Task pending_outbox_survives_a_new_store_instance_with_shared_state_via_constructor_seed()
    {
        var original = new InMemoryMessageStore();
        Envelope envelope = EnvelopeFactory.Create();
        await original.Outbox.StageAsync(envelope);
        await original.Outbox.CommitAsync();

        var rebuilt = new InMemoryMessageStore(await original.Outbox.LoadPendingAsync());

        Envelope recovered = Assert.Single(await rebuilt.Outbox.LoadPendingAsync());
        Assert.Equal(envelope.Id, recovered.Id);
    }

    [Fact]
    public async Task transaction_commit_moves_staged_to_pending()
    {
        var store = new InMemoryMessageStore();
        Envelope envelope = EnvelopeFactory.Create();
        IOutboxTransaction transaction = store.BeginOutboxTransaction();

        await transaction.StageAsync(envelope);
        await transaction.CommitAsync();

        Envelope pending = Assert.Single(await store.Outbox.LoadPendingAsync());
        Assert.Equal(envelope.Id, pending.Id);
    }

    [Fact]
    public async Task transaction_rollback_discards_staged()
    {
        var store = new InMemoryMessageStore();
        IOutboxTransaction transaction = store.BeginOutboxTransaction();
        await transaction.StageAsync(EnvelopeFactory.Create());

        await transaction.RollbackAsync();

        Assert.Empty(await store.Outbox.LoadPendingAsync());
    }

    [Fact]
    public async Task transaction_stage_after_commit_throws()
    {
        var store = new InMemoryMessageStore();
        Envelope envelope = EnvelopeFactory.Create();
        IOutboxTransaction transaction = store.BeginOutboxTransaction();
        await transaction.StageAsync(envelope);
        await transaction.CommitAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await transaction.StageAsync(envelope));
    }
}
