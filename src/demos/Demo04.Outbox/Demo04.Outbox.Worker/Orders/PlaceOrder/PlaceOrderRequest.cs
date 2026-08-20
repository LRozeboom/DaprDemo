namespace Demo04.Outbox.Worker.Orders.PlaceOrder;

/// <summary>
/// `customer` and `amount` are the order. The last two are demo switches: `failDeliveries` makes
/// the consumer reject that many deliveries, `forceConflict` makes the state transaction fail.
/// </summary>
public sealed record PlaceOrderRequest(string Customer, decimal Amount, int FailDeliveries = 0, bool ForceConflict = false);
