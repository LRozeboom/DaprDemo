using Dapr.Client;
using DaprDemos.Contracts.Messaging;
using DaprDemos.Contracts.Messaging.Events;
using DaprDemos.SharedKernel.Messaging;
using DaprDemos.SharedKernel.Results;

namespace Demo02.Retries.Subscriber.FlakyMessages.PublishFlakyMessage;

public sealed class PublishFlakyMessageCommandHandler(
    DaprClient daprClient,
    ILogger<PublishFlakyMessageCommandHandler> logger) : ICommandHandler<PublishFlakyMessageCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(PublishFlakyMessageCommand command, CancellationToken cancellationToken)
    {
        var flakyMessage = new FlakyMessageEvent(Guid.NewGuid(), command.Payload, DateTimeOffset.UtcNow);

        await daprClient.PublishEventAsync(PubSub.Name, Topics.FlakyMessages, flakyMessage, cancellationToken);

        logger.LogInformation(
            "Published flaky message {MessageId} to topic {Topic}",
            flakyMessage.Id,
            Topics.FlakyMessages);

        return flakyMessage.Id;
    }
}
