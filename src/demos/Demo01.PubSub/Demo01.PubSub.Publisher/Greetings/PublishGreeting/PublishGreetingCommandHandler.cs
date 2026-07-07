using Dapr.Client;
using DaprDemos.Contracts.Messaging;
using DaprDemos.Contracts.Messaging.Events;
using DaprDemos.SharedKernel.Messaging;
using DaprDemos.SharedKernel.Results;
// Alias needed: the Demo01.PubSub.* namespace shadows the PubSub constants class.
using Messaging = DaprDemos.Contracts.Messaging;

namespace Demo01.PubSub.Publisher.Greetings.PublishGreeting;

public sealed class PublishGreetingCommandHandler(
    DaprClient daprClient,
    ILogger<PublishGreetingCommandHandler> logger) : ICommandHandler<PublishGreetingCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(PublishGreetingCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Message))
        {
            return GreetingErrors.EmptyMessage();
        }

        var greetingSubmitted = new GreetingSubmittedEvent(Guid.NewGuid(), command.Message, DateTimeOffset.UtcNow);

        await daprClient.PublishEventAsync(Messaging.PubSub.Name, Topics.Greetings, greetingSubmitted, cancellationToken);

        logger.LogInformation(
            "Published greeting {GreetingId} to topic {Topic} via pub/sub component {PubSub}",
            greetingSubmitted.Id,
            Topics.Greetings,
            Messaging.PubSub.Name);

        return greetingSubmitted.Id;
    }
}
