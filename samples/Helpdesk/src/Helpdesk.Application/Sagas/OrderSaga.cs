using Helpdesk.Domain;
using MiniVerine.Domain.Sagas;

namespace Helpdesk.Application.Sagas;

/// <summary>
/// One in-flight order: Start places the order and cascades a ChargePayment; the
/// ChargePayment hop records the reference and cascades PaymentCharged; the final hop
/// completes the saga. All methods are instance — MiniVerine forbids static saga methods.
/// The OrderTimeout miss path (NotFound) is defined but not exercised in this slice.
/// </summary>
public sealed class OrderSaga : Saga
{
    public OrderId? Id { get; set; }

    public CustomerId? Customer { get; set; }

    public decimal Amount { get; set; }

    public PaymentReference? PaymentRef { get; set; }

    public ChargePayment Start(PlaceOrder message)
    {
        Id = message.Id;
        Customer = message.Customer;
        Amount = message.Amount;
        return new ChargePayment(
            message.Id,
            new PaymentReference(Guid.NewGuid().ToString("N")),
            message.Amount);
    }

    public PaymentCharged Handle(ChargePayment message)
    {
        PaymentRef = message.PaymentRef;
        return new PaymentCharged(message.Id, message.PaymentRef, DateTimeOffset.UtcNow);
    }

    public void Handle(PaymentCharged message)
    {
        MarkCompleted();
    }

    public void NotFound(OrderTimeout message)
    {
        // Miss path for an order that never started or already completed.
        // The OrderTimeout prove-with is the next slice.
    }
}