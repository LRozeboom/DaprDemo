using DaprDemos.SharedKernel.Messaging;
using DaprDemos.SharedKernel.Results;

namespace Demo04.Outbox.Worker.Orders.HandleOrderPlaced;

public sealed class HandleOrderPlacedCommandHandler(
    OrderStore orderStore,
    OrderDeliveryPlan deliveryPlan,
    ILogger<HandleOrderPlacedCommandHandler> logger) : ICommandHandler<HandleOrderPlacedCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(HandleOrderPlacedCommand command, CancellationToken cancellationToken)
    {
        var delivery = deliveryPlan.Next(command.Id);

        if (delivery.ShouldFail)
        {
            logger.LogWarning(
                "Failed Attempt {Attempt} of {PlannedFailures} for order {OrderId}: failing delivery on purpose — Dapr will redeliver",
                delivery.Attempt,
                delivery.PlannedFailures,
                command.Id);

            return OrderErrors.DeliveryFailed(command.Id);
        }

        // The payoff line: the row is already there. The event cannot arrive before the write that
        // produced it has committed, so a consumer never sees an order the database does not have.
        var stored = await orderStore.GetAsync(command.Id, cancellationToken);

        logger.LogInformation(
            "ORDER RECEIVED {OrderId}: {Customer} for {Amount} — the state store already has it as '{Status}' (attempt {Attempt})",
            command.Id,
            command.Customer,
            command.Amount,
            stored?.Status ?? "MISSING",
            delivery.Attempt);

        return Unit.Value;
    }
}
