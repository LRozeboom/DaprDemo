namespace Demo04.Outbox.Worker.Orders.PlaceOrder;

/// <summary>An order is just a customer and an amount — no demo switches.</summary>
public sealed record PlaceOrderRequest(string Customer, decimal Amount);
