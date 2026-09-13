using System.Diagnostics;
using Hansom.Application.Discovery;
using Hansom.Application.Execution;
using Hansom.Domain.Envelope;

namespace Hansom.Infrastructure.Observability;

/// <summary>
/// <see cref="IHandlerAttemptObserver"/> that emits one <see cref="Activity"/> per handler
/// attempt and increments the failure counter when the attempt throws. A no-op exporter:
/// nothing is exported until an external listener subscribes via <see cref="HansomDiagnostics"/>.
/// </summary>
public sealed class ObservabilityAttemptObserver : IHandlerAttemptObserver
{
    public void OnAttempt(Envelope envelope, DiscoveredHandler handler, Exception? exception)
    {
        using Activity? activity = HansomDiagnostics.Source.StartActivity(
            "Hansom.Handler.Attempt",
            ActivityKind.Internal);

        activity?.SetTag("hansom.message.type", handler.MessageClrType.FullName);
        activity?.SetTag("hansom.destination", envelope.Destination.Value);
        activity?.SetTag("hansom.attempt", envelope.Attempts.Value);
        activity?.SetTag("hansom.envelope.id", envelope.Id.Value.ToString());

        if (exception is null)
        {
            return;
        }

        activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
        HansomDiagnostics.Failures.Add(1);
    }
}
