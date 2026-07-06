using DaprDemos.Contracts.Messaging.Events;

namespace Demo01.PubSub.Subscriber.Greetings.HandleGreeting;

public static class Mappers
{
    public static HandleGreetingCommand ToCommand(this GreetingSubmittedEvent greetingSubmitted) =>
        new(greetingSubmitted.Id, greetingSubmitted.Message, greetingSubmitted.SubmittedAt);
}
