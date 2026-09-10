using System.Diagnostics;
using System.Threading.Channels;
using MiniVerine.Application.Discovery;
using MiniVerine.Application.Execution;
using MiniVerine.Domain.Envelope;

namespace MiniVerine.Infrastructure.LocalQueues;

/// <summary>
/// One in-process queue worker for a destination. Owns a channel and a single reader task.
/// v1 is unbounded; back-pressure is a future slice. A durable queue is this agent plus Persistence.
/// </summary>
public sealed class LocalQueueAgent
{
    private readonly Channel<Envelope> _channel = Channel.CreateUnbounded<Envelope>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
    private readonly HandlerCatalog _catalog;
    private readonly Executor _executor;
    private volatile TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private volatile bool _paused;
    private volatile Task? _worker;
    private int _started;

    public LocalQueueAgent(string name, HandlerCatalog catalog, Executor executor)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(executor);
        Name = name;
        _catalog = catalog;
        _executor = executor;
    }

    public string Name { get; }

    public int QueueLength => _channel.Reader.Count;

    public void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) == 1)
        {
            return;
        }

        _worker = Task.Run(RunAsync);
    }

    public void Enqueue(Envelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        Start();
        _channel.Writer.TryWrite(envelope);
    }

    public void Pause()
    {
        _gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _paused = true;
    }

    public void Resume()
    {
        _paused = false;
        _gate.TrySetResult();
    }

    public async Task DrainAsync(CancellationToken cancellationToken)
    {
        _channel.Writer.TryComplete();
        Task? worker = _worker;
        if (worker is not null)
        {
            await worker.WaitAsync(cancellationToken);
        }
    }

    private async Task RunAsync()
    {
        while (await _channel.Reader.WaitToReadAsync())
        {
            while (_channel.Reader.TryRead(out Envelope? envelope))
            {
                await WaitWhilePausedAsync();
                try
                {
                    await ProcessAsync(envelope);
                }
                catch (Exception exception)
                {
                    Trace.TraceError($"Local queue '{Name}' failed handling an envelope: {exception}");
                }
            }
        }
    }

    private Task WaitWhilePausedAsync() => _paused ? _gate.Task : Task.CompletedTask;

    private async Task ProcessAsync(Envelope envelope)
    {
        HandlerLookup lookup = _catalog.Lookup(envelope.Message.Value.GetType());
        if (lookup is MissingHandler)
        {
            await _executor.HandleMissingAsync(envelope);
            return;
        }

        foreach (DiscoveredHandler handler in ((FoundHandlers)lookup).Handlers)
        {
            await _executor.InvokeAsync(envelope, handler with { Scheduled = true });
        }
    }
}
