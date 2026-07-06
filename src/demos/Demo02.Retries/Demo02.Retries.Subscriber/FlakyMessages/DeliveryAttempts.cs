using System.Collections.Concurrent;

namespace Demo02.Retries.Subscriber.FlakyMessages;

/// <summary>Counts deliveries per message id so redeliveries are visible and countable in the logs.</summary>
public sealed class DeliveryAttempts
{
    private readonly ConcurrentDictionary<Guid, int> _attempts = new();

    public int Next(Guid messageId) => _attempts.AddOrUpdate(messageId, 1, (_, attempts) => attempts + 1);
}
