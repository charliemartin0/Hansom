using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniVerine.Application.Bus;
using MiniVerine.Infrastructure.Hosting;

namespace MiniVerine.Tests.Infrastructure.Hosting;

public sealed class HostingTests
{
    private static IHost BuildHost()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.UseMiniVerine();
        return builder.Build();
    }

    [Fact]
    public void host_with_use_miniverine_registers_miniverine_hosted_service()
    {
        using IHost host = BuildHost();

        IEnumerable<IHostedService> services = host.Services.GetServices<IHostedService>();

        Assert.Contains(services, service => service is MiniVerineHostedService);
    }

    [Fact]
    public void host_with_use_miniverine_registers_message_bus_as_singleton()
    {
        using IHost host = BuildHost();

        IMessageBus first = host.Services.GetRequiredService<IMessageBus>();
        IMessageBus second = host.Services.GetRequiredService<IMessageBus>();

        Assert.True(ReferenceEquals(first, second));
    }

    [Fact]
    public async Task host_starts_and_stops_cleanly_with_no_messages()
    {
        using IHost host = BuildHost();

        await host.StartAsync();
        await host.StopAsync();

        MiniVerineHostedService service = Assert.Single(
            host.Services.GetServices<IHostedService>().OfType<MiniVerineHostedService>());
        Assert.Equal(1, service.StartCount);
        Assert.Equal(1, service.StopCount);
    }
}