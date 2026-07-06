namespace Demo02.Retries.Subscriber.FlakyMessages.HandleFlakyMessage;

public sealed record HandleFlakyMessageCommand(Guid Id, string Payload, DateTimeOffset SubmittedAt);
