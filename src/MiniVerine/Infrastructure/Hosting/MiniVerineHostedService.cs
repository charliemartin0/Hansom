using Microsoft.Extensions.Hosting;

namespace MiniVerine.Infrastructure.Hosting;

/// <summary>
/// IHostedService that starts listeners / durability agents and drains on StopAsync.
/// No listeners/agents exist yet, so StartAsync/StopAsync only track lifecycle.
/// </summary>
public sealed class MiniVerineHostedService : IHostedService
{
    public int StartCount { get; private set; }

    public int StopCount { get; private set; }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        StartCount++;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        StopCount++;
        return Task.CompletedTask;
    }
}