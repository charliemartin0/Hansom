using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniVerine.Application.Bus;
using MiniVerine.Application.Discovery;
using MiniVerine.Application.Execution;
using MiniVerine.Application.Mediator;
using MiniVerine.Application.Middleware;
using MiniVerine.Application.Persistence;
using MiniVerine.Application.Routing;
using MiniVerine.Application.Sagas;
using MiniVerine.Application.Scheduling;
using MiniVerine.Application.Serialization;
using MiniVerine.Application.Tracking;
using MiniVerine.Application.Transports;
using MiniVerine.Infrastructure.Hosting;
using MiniVerine.Infrastructure.LocalQueues;
using MiniVerine.Infrastructure.Persistence;
using MiniVerine.Infrastructure.Serialization;
using MiniVerine.Infrastructure.Transports;

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
        builder.Services.AddSingleton<HandlerCatalog>(services =>
        {
            var catalog = new HandlerCatalog();
            foreach (Assembly assembly in options.HandlerAssemblies)
            {
                catalog.Scan(assembly);
            }

            return catalog;
        });
        builder.Services.AddSingleton(options.Policies);
        builder.Services.AddSingleton<MiddlewareCatalog>();
        builder.Services.AddSingleton(options.Routing);
        builder.Services.AddSingleton<ISagaStore, InMemorySagaStore>();
        builder.Services.AddSingleton<IScheduledEnvelopeHold, InMemoryScheduledEnvelopeHold>();
        builder.Services.AddSingleton<IHandlerAttemptObserver, AttemptObserverHub>();
        builder.Services.AddSingleton<ISerializer, JsonSerializer>();
        builder.Services.AddSingleton<IMessageStore, InMemoryMessageStore>();
        builder.Services.AddSingleton<Executor>(services => new Executor(
            services.GetRequiredService<ErrorPolicyCatalog>(),
            errorQueue: services.GetService<IErrorQueue>(),
            missingHandler: services.GetService<IMissingHandler>(),
            middleware: services.GetRequiredService<MiddlewareCatalog>(),
            scheduled: services.GetRequiredService<IScheduledEnvelopeHold>(),
            attempts: services.GetRequiredService<IHandlerAttemptObserver>()));
        builder.Services.AddSingleton<MessageDelivery>(services => new MessageDelivery(
            services.GetRequiredService<HandlerCatalog>(),
            services.GetRequiredService<Executor>(),
            hold: services.GetRequiredService<IScheduledEnvelopeHold>(),
            routing: services.GetRequiredService<RoutingCatalog>(),
            transport: () => services.GetRequiredService<ITransport>()));
        builder.Services.AddSingleton<LocalQueueCatalog>();
        builder.Services.AddSingleton<IPublishEnqueuer>(
            services => services.GetRequiredService<LocalQueueCatalog>());
        builder.Services.AddSingleton<ITransport>(services => new LocalTransport(
            services.GetRequiredService<IPublishEnqueuer>(),
            services.GetRequiredService<MessageDelivery>()));
        builder.Services.AddSingleton<Mediator>(services => new Mediator(
            services.GetRequiredService<HandlerCatalog>(),
            executor: services.GetRequiredService<Executor>(),
            hold: services.GetRequiredService<IScheduledEnvelopeHold>(),
            sagas: services.GetRequiredService<ISagaStore>(),
            routing: services.GetRequiredService<RoutingCatalog>(),
            enqueuer: services.GetRequiredService<IPublishEnqueuer>(),
            attempts: services.GetRequiredService<IHandlerAttemptObserver>()));
        builder.Services.AddSingleton<IMessageBus>(services => services.GetRequiredService<Mediator>());
        builder.Services.AddSingleton<MiniVerineHostedService>();
        builder.Services.AddSingleton<IHostedService>(
            services => services.GetRequiredService<MiniVerineHostedService>());
        return builder;
    }
}