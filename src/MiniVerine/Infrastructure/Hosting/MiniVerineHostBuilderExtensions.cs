using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniVerine.Application.Bus;
using MiniVerine.Application.Discovery;
using MiniVerine.Application.Execution;
using MiniVerine.Application.Mediator;
using MiniVerine.Application.Routing;
using MiniVerine.Infrastructure.Hosting;
using MiniVerine.Infrastructure.LocalQueues;

namespace MiniVerine;

public static class MiniVerineHostBuilderExtensions
{
    public static IHostApplicationBuilder UseMiniVerine(
        this IHostApplicationBuilder builder,
        Action<MiniVerineOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new MiniVerineOptions();
        configure?.Invoke(options);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<HandlerCatalog>();
        builder.Services.AddSingleton<ErrorPolicyCatalog>();
        builder.Services.AddSingleton<RoutingCatalog>();
        builder.Services.AddSingleton<LocalQueueCatalog>();
        builder.Services.AddSingleton<IPublishEnqueuer>(
            services => services.GetRequiredService<LocalQueueCatalog>());
        builder.Services.AddSingleton<Mediator>();
        builder.Services.AddSingleton<IMessageBus>(services => services.GetRequiredService<Mediator>());
        builder.Services.AddSingleton<MiniVerineHostedService>();
        builder.Services.AddSingleton<IHostedService>(
            services => services.GetRequiredService<MiniVerineHostedService>());
        return builder;
    }
}
