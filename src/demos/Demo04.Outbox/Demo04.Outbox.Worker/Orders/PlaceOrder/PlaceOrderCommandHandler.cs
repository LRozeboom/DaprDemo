using DaprDemos.Contracts.Messaging.Events;
using DaprDemos.SharedKernel.Messaging;
using DaprDemos.SharedKernel.Results;

namespace Demo04.Outbox.Worker.Orders.PlaceOrder;

public sealed class PlaceOrderCommandHandler(
    OrderStore orderStore,
    ILogger<PlaceOrderCommandHandler> logger) : ICommandHandler<PlaceOrderCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Customer))
        {
            return OrderErrors.EmptyCustomer();
        }

        if (command.Amount <= 0)
        {
            return OrderErrors.NonPositiveAmount();
        }

        var order = new OrderRecord(
            Guid.NewGuid(),
            command.Customer,
            command.Amount,
            DateTimeOffset.UtcNow,
            OrderRecord.PlacedStatus);

        var orderPlaced = new OrderPlacedEvent(order.Id, order.Customer, order.Amount, order.PlacedAt);

        // One call. No PublishEventAsync anywhere in this demo — the event is part of the
        // transaction, and daprd puts it on the topic only after the transaction commits.
        await orderStore.CommitAsync(order, orderPlaced, cancellationToken);

        logger.LogInformation(
            "Committed order {OrderId} for {Customer} ({Amount}) under key {Key} — the OrderPlaced event rode along in the same transaction",
            order.Id,
            order.Customer,
            order.Amount,
            OrderStore.KeyFor(order.Id));

        return order.Id;
    }
}
