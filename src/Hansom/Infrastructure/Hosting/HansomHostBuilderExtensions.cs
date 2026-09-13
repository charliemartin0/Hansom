using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Hansom.Application.Bus;
using Hansom.Application.Discovery;
using Hansom.Application.Discovery.Validators;
using Hansom.Application.Execution;
using Hansom.Application.Mediator;
using Hansom.Application.Middleware;
using Hansom.Application.Persistence;
using Hansom.Application.Routing;
using Hansom.Application.Sagas;
using Hansom.Application.Scheduling;
using Hansom.Application.Serialization;
using Hansom.Application.Tracking;
using Hansom.Application.Transports;
using Hansom.Infrastructure.Hosting;
using Hansom.Infrastructure.LocalQueues;
using Hansom.Infrastructure.Persistence;
using Hansom.Infrastructure.Sagas;
using Hansom.Infrastructure.Scheduling;
using Hansom.Infrastructure.Serialization;
using Hansom.Infrastructure.Transports;

namespace Hansom;

public static class HansomHostBuilderExtensions
{
    public static IHostApplicationBuilder UseHansom(
        this IHostApplicationBuilder builder,
        Action<HansomOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new HansomOptions();
        configure?.Invoke(options);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<HandlerCatalog>(services =>
        {
            var catalog = new HandlerCatalog();
            foreach (Assembly assembly in options.HandlerAssemblies)
            {
                catalog.Scan(assembly);
            }

            FluentValidation.Results.ValidationResult validation = new HandlerCatalogValidator().Validate(catalog);
            if (!validation.IsValid)
            {
                throw new ValidationException(validation.Errors);
            }

            return catalog;
        });
        builder.Services.AddSingleton(options.Policies);
        builder.Services.AddSingleton<IOutboxStore>(
            services => services.GetRequiredService<IMessageStore>().Outbox);
        builder.Services.AddSingleton<TransactionalOutboxMiddleware>();
        builder.Services.AddSingleton<MiddlewareCatalog>(services =>
        {
            var catalog = new MiddlewareCatalog();
            if (options.EnableTransactionalOutbox)
            {
                catalog.Register(MiddlewareLayer.Inner, services.GetRequiredService<TransactionalOutboxMiddleware>());
            }

            return catalog;
        });
        builder.Services.AddSingleton(options.Routing);
        builder.Services.AddSingleton<ISagaStore, InMemorySagaStore>();
        builder.Services.AddSingleton<IScheduledEnvelopeHold, InMemoryScheduledEnvelopeHold>();
        builder.Services.AddSingleton<IHandlerAttemptObserver, AttemptObserverHub>();
        builder.Services.AddSingleton<ISerializer, JsonSerializer>();
        builder.Services.AddSingleton<IMessageStore, InMemoryMessageStore>();
        builder.Services.AddSingleton<IDeadLetterStore>(
            services => services.GetRequiredService<IMessageStore>().DeadLetter);
        builder.Services.AddSingleton<IErrorQueue>(services => new DeadLetterQueueAdapter(
            services.GetRequiredService<IDeadLetterStore>()));
        builder.Services.AddSingleton<Executor>(services => new Executor(
            services.GetRequiredService<ErrorPolicyCatalog>(),
            errorQueue: services.GetService<IErrorQueue>(),
            missingHandler: services.GetService<IMissingHandler>(),
            middleware: services.GetRequiredService<MiddlewareCatalog>(),
            scheduled: services.GetRequiredService<IScheduledEnvelopeHold>(),
            attempts: services.GetRequiredService<IHandlerAttemptObserver>(),
            enqueuer: () => services.GetRequiredService<IPublishEnqueuer>()));
        builder.Services.AddSingleton<MessageDelivery>(services => new MessageDelivery(
            services.GetRequiredService<HandlerCatalog>(),
            services.GetRequiredService<Executor>(),
            hold: services.GetRequiredService<IScheduledEnvelopeHold>(),
            routing: services.GetRequiredService<RoutingCatalog>(),
            transport: () => services.GetRequiredService<ITransport>()));
        builder.Services.AddSingleton<LocalQueueCatalog>(services => new LocalQueueCatalog(
            (envelope, cancellationToken) => services.GetRequiredService<Mediator>()
                .DispatchHandlers(envelope, scheduled: true, cancellationToken)));
        builder.Services.AddSingleton<IPublishEnqueuer>(
            services => services.GetRequiredService<LocalQueueCatalog>());
        builder.Services.AddSingleton<ITransport>(services => new LocalTransport(
            services.GetRequiredService<IPublishEnqueuer>(),
            (envelope, cancellationToken) => services.GetRequiredService<Mediator>()
                .DispatchHandlers(envelope, scheduled: true, cancellationToken)));
        builder.Services.AddSingleton<Mediator>(services => new Mediator(
            services.GetRequiredService<HandlerCatalog>(),
            executor: services.GetRequiredService<Executor>(),
            hold: services.GetRequiredService<IScheduledEnvelopeHold>(),
            sagas: services.GetRequiredService<ISagaStore>(),
            routing: services.GetRequiredService<RoutingCatalog>(),
            enqueuer: services.GetRequiredService<IPublishEnqueuer>(),
            attempts: services.GetRequiredService<IHandlerAttemptObserver>()));
        builder.Services.AddSingleton<IMessageBus>(services => services.GetRequiredService<Mediator>());
        builder.Services.AddSingleton<HansomHostedService>();
        builder.Services.AddSingleton<IHostedService>(
            services => services.GetRequiredService<HansomHostedService>());
        return builder;
    }
}