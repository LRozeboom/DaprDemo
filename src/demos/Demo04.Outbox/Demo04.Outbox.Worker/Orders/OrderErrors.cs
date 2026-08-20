using DaprDemos.SharedKernel.Results;

namespace Demo04.Outbox.Worker.Orders;

public static class OrderErrors
{
    public const string TransactionRejectedCode = "Order.TransactionRejected";

    public static Error EmptyCustomer() =>
        new("Order.EmptyCustomer", "An order must name a customer.");

    public static Error NonPositiveAmount() =>
        new("Order.NonPositiveAmount", "An order amount must be greater than zero.");

    public static Error TransactionRejected(Guid orderId, string reason) =>
        new(TransactionRejectedCode, $"The state transaction for order {orderId} was rejected, so nothing was stored and nothing will be published. {reason}");

    public static Error NotFound(Guid orderId) =>
        new("Order.NotFound", $"No order {orderId} in the state store.");

    public static Error DeliveryFailed(Guid orderId) =>
        new("Order.DeliveryFailed", $"Simulated failure while consuming the OrderPlaced event for order {orderId}.");
}
