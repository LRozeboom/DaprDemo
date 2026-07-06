using DaprDemos.Contracts.Messaging.Events;

namespace Demo02.Retries.Subscriber.FlakyMessages.HandleFlakyMessage;

public static class Mappers
{
    public static HandleFlakyMessageCommand ToCommand(this FlakyMessageEvent flakyMessage) =>
        new(flakyMessage.Id, flakyMessage.Payload, flakyMessage.SubmittedAt);
}
