using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Hansom.Application.Bus;
using Hansom.Infrastructure.Hosting;

namespace Hansom.Tests.Infrastructure.Hosting;

public sealed class HostingTests
{
    private static IHost BuildHost()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.UseHansom();
        return builder.Build();
    }

    [Fact]
    public void host_with_use_hansom_registers_hansom_hosted_service()
    {
        using IHost host = BuildHost();

        IEnumerable<IHostedService> services = host.Services.GetServices<IHostedService>();

        Assert.Contains(services, service => service is HansomHostedService);
    }

    [Fact]
    public void host_with_use_hansom_registers_message_bus_as_singleton()
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

        HansomHostedService service = Assert.Single(
            host.Services.GetServices<IHostedService>().OfType<HansomHostedService>());
        Assert.Equal(1, service.StartCount);
        Assert.Equal(1, service.StopCount);
    }
}