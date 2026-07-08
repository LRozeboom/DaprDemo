namespace DaprDemos.Contracts.Messaging.Events;

public sealed record GreetingSubmittedEvent(Guid Id, string Message, DateTimeOffset SubmittedAt);
