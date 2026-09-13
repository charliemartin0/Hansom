using MiniVerine.Domain.Messaging;

namespace MiniVerine.Tests.InvalidCatalog;

/// <summary>
/// An empty wire-name alias violates the MessageTypeValidator the HandlerCatalogValidator
/// composes. Lives in its own never-scanned fixture assembly (a Scan-time-invalid handler
/// would throw before the catalog validation runs).
/// </summary>
[MessageIdentity("")]
public sealed record EmptyAliasScanMessage;

public sealed class EmptyAliasScanHandler
{
    public void Handle(EmptyAliasScanMessage message)
    {
    }
}