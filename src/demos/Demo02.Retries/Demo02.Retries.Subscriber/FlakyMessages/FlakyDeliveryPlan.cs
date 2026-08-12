using System.Collections.Concurrent;

namespace Demo02.Retries.Subscriber.FlakyMessages;

/// <summary>
/// Decides, per message, how many deliveries fail before one succeeds (a random 1-5) and counts the
/// attempts, so redeliveries are visible and countable in the logs. There is no arming step: publish
/// a message and the first delivery rolls the dice for it.
/// </summary>
public sealed class FlakyDeliveryPlan
{
    private const int MinFailures = 1;
    private const int MaxFailures = 5;

    private readonly ConcurrentDictionary<Guid, MessagePlan> _plans = new();

    public FlakyDelivery Next(Guid messageId)
    {
        var plan = _plans.GetOrAdd(messageId, _ => new MessagePlan(Random.Shared.Next(MinFailures, MaxFailures + 1)));

        return new FlakyDelivery(plan.NextAttempt(), plan.PlannedFailures);
    }

    private sealed class MessagePlan(int plannedFailures)
    {
        private int _attempts;

        public int PlannedFailures { get; } = plannedFailures;

        public int NextAttempt() => Interlocked.Increment(ref _attempts);
    }
}

public readonly record struct FlakyDelivery(int Attempt, int PlannedFailures)
{
    public bool ShouldFail => Attempt <= PlannedFailures;
}
