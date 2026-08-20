namespace Demo04.Outbox.Worker.Orders.PlaceOrder;

public sealed record PlaceOrderCommand(string Customer, decimal Amount);
