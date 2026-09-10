using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniVerine.Application.Bus;
using MiniVerine.Application.Discovery;
using MiniVerine.Application.Mediator;
using MiniVerine.Infrastructure.Hosting;

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
        builder.Services.AddSingleton<Mediator>();
        builder.Services.AddSingleton<IMessageBus>(services => services.GetRequiredService<Mediator>());
        builder.Services.AddSingleton<MiniVerineHostedService>();
        builder.Services.AddSingleton<IHostedService>(
            services => services.GetRequiredService<MiniVerineHostedService>());
        return builder;
    }
}
