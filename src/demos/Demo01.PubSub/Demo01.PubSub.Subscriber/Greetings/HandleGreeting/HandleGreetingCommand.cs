namespace Demo01.PubSub.Subscriber.Greetings.HandleGreeting;

public sealed record HandleGreetingCommand(Guid Id, string Message, DateTimeOffset SubmittedAt);
