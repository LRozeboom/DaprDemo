using DaprDemos.SharedKernel.Results;

namespace Demo04.Outbox.Worker.Orders;

public static class OrderErrors
{
    public static Error EmptyCustomer() =>
        new("Order.EmptyCustomer", "An order must name a customer.");

    public static Error NonPositiveAmount() =>
        new("Order.NonPositiveAmount", "An order amount must be greater than zero.");

    public static Error NotFound(Guid orderId) =>
        new("Order.NotFound", $"No order {orderId} in the state store.");
}
