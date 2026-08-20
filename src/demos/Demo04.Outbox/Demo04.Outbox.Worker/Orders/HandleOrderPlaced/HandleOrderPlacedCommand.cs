namespace Demo04.Outbox.Worker.Orders.HandleOrderPlaced;

public sealed record HandleOrderPlacedCommand(Guid Id, string Customer, decimal Amount, DateTimeOffset PlacedAt);
