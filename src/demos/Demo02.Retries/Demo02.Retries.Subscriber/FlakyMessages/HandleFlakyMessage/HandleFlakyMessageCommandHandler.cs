using DaprDemos.SharedKernel.Messaging;
using DaprDemos.SharedKernel.Results;

namespace Demo02.Retries.Subscriber.FlakyMessages.HandleFlakyMessage;

public sealed class HandleFlakyMessageCommandHandler(
    FlakyDeliveryPlan deliveryPlan,
    ILogger<HandleFlakyMessageCommandHandler> logger) : ICommandHandler<HandleFlakyMessageCommand, Unit>
{
    public Task<Result<Unit>> HandleAsync(HandleFlakyMessageCommand command, CancellationToken cancellationToken)
    {
        var delivery = deliveryPlan.Next(command.Id);

        if (delivery.ShouldFail)
        {
            logger.LogWarning(
                "Failed Attempt {Attempt} of {PlannedFailures} for message {MessageId}: failing delivery on purpose — Dapr will redeliver",
                delivery.Attempt,
                delivery.PlannedFailures,
                command.Id);

            return Task.FromResult<Result<Unit>>(FlakyMessageErrors.SimulatedFailure(command.Id));
        }

        logger.LogInformation(
            "Succeeded Attempt {Attempt}: processed message {MessageId} after {PlannedFailures} failed deliveries",
            delivery.Attempt,
            command.Id,
            delivery.PlannedFailures);

        return Task.FromResult<Result<Unit>>(Unit.Value);
    }
}
