using System.Diagnostics;
using MiniVerine.Application.Discovery;
using MiniVerine.Application.Execution;
using MiniVerine.Domain.Envelope;

namespace MiniVerine.Infrastructure.Observability;

/// <summary>
/// <see cref="IHandlerAttemptObserver"/> that emits one <see cref="Activity"/> per handler
/// attempt and increments the failure counter when the attempt throws. A no-op exporter:
/// nothing is exported until an external listener subscribes via <see cref="MiniVerineDiagnostics"/>.
/// </summary>
public sealed class ObservabilityAttemptObserver : IHandlerAttemptObserver
{
    public void OnAttempt(Envelope envelope, DiscoveredHandler handler, Exception? exception)
    {
        using Activity? activity = MiniVerineDiagnostics.Source.StartActivity(
            "MiniVerine.Handler.Attempt",
            ActivityKind.Internal);

        activity?.SetTag("miniverine.message.type", handler.MessageClrType.FullName);
        activity?.SetTag("miniverine.destination", envelope.Destination.Value);
        activity?.SetTag("miniverine.attempt", envelope.Attempts.Value);
        activity?.SetTag("miniverine.envelope.id", envelope.Id.Value.ToString());

        if (exception is null)
        {
            return;
        }

        activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
        MiniVerineDiagnostics.Failures.Add(1);
    }
}
