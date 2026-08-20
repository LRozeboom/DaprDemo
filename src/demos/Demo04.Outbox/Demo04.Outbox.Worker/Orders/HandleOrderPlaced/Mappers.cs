using DaprDemos.Contracts.Messaging.Events;

namespace Demo04.Outbox.Worker.Orders.HandleOrderPlaced;

public static class Mappers
{
    public static HandleOrderPlacedCommand ToCommand(this OrderPlacedEvent orderPlaced) =>
        new(orderPlaced.Id, orderPlaced.Customer, orderPlaced.Amount, orderPlaced.PlacedAt);
}
