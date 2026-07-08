namespace DaprDemos.Contracts.Messaging.Events;

public sealed record FlakyMessageEvent(Guid Id, string Payload, DateTimeOffset SubmittedAt);
