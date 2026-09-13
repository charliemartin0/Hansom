using Hansom.Application.Discovery;
using Hansom.Domain.Envelope;

namespace Hansom.Application.Execution;

/// <summary>
/// One call per handler attempt (success or throw). Tracking uses this for Executed bags.
/// </summary>
public interface IHandlerAttemptObserver
{
    void OnAttempt(Envelope envelope, DiscoveredHandler handler, Exception? exception);
}
