using Helpdesk.Application.Sagas;
using Helpdesk.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniVerine;
using MiniVerine.Application.Bus;
using MiniVerine.Application.Tracking;

namespace Helpdesk.Tests.Application;

/// <summary>
/// Prove-with for the Helpdesk sample: the smallest in-memory conversation through
/// MiniVerine's public surface — PlaceOrder starts an OrderSaga whose cascades run
/// ChargePayment and then PaymentCharged inside one tracked session.
/// </summary>
public sealed class OrderConversationTests
{
    [Fact]
    public async Task invoke_tracked_place_order_cascades_to_charge_payment()
    {
        using IHost host = await StartedHostAsync();
        IMessageBus bus = host.Services.GetRequiredService<IMessageBus>();

        TrackedSession session = await bus.InvokeTrackedAsync(NewPlaceOrder());

        Assert.Contains(session.Executed, executed => executed.Message is PlaceOrder);
        Assert.Contains(session.Executed, executed => executed.Message is ChargePayment);
    }

    [Fact]
    public async Task invoke_tracked_full_conversation_includes_payment_charged()
    {
        using IHost host = await StartedHostAsync();
        IMessageBus bus = host.Services.GetRequiredService<IMessageBus>();

        TrackedSession session = await bus.InvokeTrackedAsync(NewPlaceOrder());

        Assert.Equal(
            [typeof(PlaceOrder), typeof(ChargePayment), typeof(PaymentCharged)],
            session.Executed.Select(executed => executed.Message.GetType()));
    }

    private static async Task<IHost> StartedHostAsync()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.UseMiniVerine(options =>
        {
            options.HandlerAssemblies.Add(typeof(PlaceOrder).Assembly);
            options.HandlerAssemblies.Add(typeof(OrderSaga).Assembly);
        });
        IHost host = builder.Build();
        await host.StartAsync();
        return host;
    }

    private static PlaceOrder NewPlaceOrder() =>
        new(new OrderId(1), new CustomerId(Guid.NewGuid()), 42.5m, DateTimeOffset.UtcNow);
}