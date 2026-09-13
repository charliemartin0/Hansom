using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniVerine.Application.Bus;
using MiniVerine.Application.Sagas;
using MiniVerine.Domain.Sagas.ValueObjects;
using MiniVerine.Tests.Handlers;

namespace MiniVerine.Tests.Infrastructure.LocalQueues;

/// <summary>
/// Prove-with for the queue/transport saga-dispatch fix: a saga message published
/// through the local queue must route through the saga orchestration (load/save),
/// not the plain executor path that mutates a fresh instance and drops the state.
/// </summary>
public sealed class LocalQueueSagaTests
{
    [Fact]
    public async Task local_queue_handles_a_saga_message_with_persisted_state()
    {
        QueueSaga.Reset();
        var builder = Host.CreateApplicationBuilder();
        builder.UseMiniVerine(options =>
        {
            options.HandlerAssemblies.Add(typeof(SagasViaQueueMessage).Assembly);
            options.PublishMessage<SagasViaQueueMessage>().ToLocalQueue("saga-queue");
        });
        using IHost host = builder.Build();
        await host.StartAsync();
        try
        {
            IMessageBus bus = host.Services.GetRequiredService<IMessageBus>();
            ISagaStore store = host.Services.GetRequiredService<ISagaStore>();

            // Start the saga so the Handle path has a row to load.
            await bus.InvokeAsync(new QueueSagaStart(1, "started"));

            await bus.PublishAsync(new SagasViaQueueMessage(1, "processed"));
            await QueueSaga.Handled.WaitAsync(TimeSpan.FromSeconds(5));

            // The mutation must have landed in the store, not on an ephemeral instance.
            var saga = Assert.IsType<QueueSaga>(store.Load(typeof(QueueSaga), new SagaId("1")));
            Assert.Equal("processed", saga.Status);
        }
        finally
        {
            await host.StopAsync();
        }
    }
}