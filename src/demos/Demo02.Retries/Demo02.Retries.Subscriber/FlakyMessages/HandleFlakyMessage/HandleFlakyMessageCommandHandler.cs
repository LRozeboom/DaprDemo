using DaprDemos.SharedKernel.Messaging;
using DaprDemos.SharedKernel.Results;

namespace Demo02.Retries.Subscriber.FlakyMessages.HandleFlakyMessage;

public sealed class HandleFlakyMessageCommandHandler(
    FailureToggle failureToggle,
    DeliveryAttempts deliveryAttempts,
    ILogger<HandleFlakyMessageCommandHandler> logger) : ICommandHandler<HandleFlakyMessageCommand, Unit>
{
    public Task<Result<Unit>> HandleAsync(HandleFlakyMessageCommand command, CancellationToken cancellationToken)
    {
        var attempt = deliveryAttempts.Next(command.Id);

        if (failureToggle.TryConsumeFailure())
        {
            logger.LogWarning(
                "Failed Attempt {Attempt} for message {MessageId}: failing delivery on purpose — Dapr will redeliver",
                attempt,
                command.Id);

            return Task.FromResult<Result<Unit>>(FlakyMessageErrors.SimulatedFailure(command.Id));
        }

        logger.LogInformation(
            "Succeeded Attempt {Attempt}: processed message {MessageId} after retries",
            attempt,
            command.Id);

        return Task.FromResult<Result<Unit>>(Unit.Value);
    }
}
