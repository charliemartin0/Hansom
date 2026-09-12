using System.Globalization;

namespace Helpdesk.Domain;

/// <summary>
/// Order identity. ToString yields the bare value so saga correlation produces
/// readable saga ids ("1", not "OrderId { Value = 1 }").
/// </summary>
public sealed record OrderId(int Value)
{
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

public sealed record CustomerId(Guid Value);

public sealed record PaymentReference(string Value);

/// <summary>
/// Starts an order conversation. Saga correlation uses the Id property (the
/// convention fallback — Helpdesk.Domain carries no MiniVerine attributes).
/// </summary>
public sealed record PlaceOrder(OrderId Id, CustomerId Customer, decimal Amount, DateTimeOffset PlacedAt);

public sealed record ChargePayment(OrderId Id, PaymentReference PaymentRef, decimal Amount);

public sealed record PaymentCharged(OrderId Id, PaymentReference PaymentRef, DateTimeOffset ChargedAt);

/// <summary>
/// Defined for the saga miss path (NotFound); not scheduled or exercised in the
/// in-memory conversation slice — scheduling needs a delay mechanism the
/// MiniVerine-free Domain cannot express.
/// </summary>
public sealed record OrderTimeout(OrderId Id);