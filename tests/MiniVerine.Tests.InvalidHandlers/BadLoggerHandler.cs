using Microsoft.Extensions.Logging;

namespace MiniVerine.Tests.InvalidHandlers;

/// <summary>
/// A deliberately invalid handler: the second parameter is an ILogger injection slot
/// that MiniVerine cannot fill. Scan must reject it at discovery time, not null-fill
/// it at invocation. Lives in its own assembly so no other test's host scan touches it.
/// </summary>
public sealed record BadLoggerCommand(int OrderId);

public sealed class BadLoggerHandler
{
    public void Handle(BadLoggerCommand message, ILogger<BadLoggerHandler> logger)
    {
    }
}