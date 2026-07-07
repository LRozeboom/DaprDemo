using DaprDemos.SharedKernel.Messaging;
using DaprDemos.SharedKernel.Results;

namespace Demo01.PubSub.Subscriber.Greetings.HandleGreeting;

public sealed class HandleGreetingCommandHandler(
    ILogger<HandleGreetingCommandHandler> logger) : ICommandHandler<HandleGreetingCommand, Unit>
{
    public Task<Result<Unit>> HandleAsync(HandleGreetingCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "GREETING RECEIVED: \"{Message}\" (id {GreetingId}, submitted {SubmittedAt:O})",
            command.Message,
            command.Id,
            command.SubmittedAt);

        return Task.FromResult<Result<Unit>>(Unit.Value);
    }
}
