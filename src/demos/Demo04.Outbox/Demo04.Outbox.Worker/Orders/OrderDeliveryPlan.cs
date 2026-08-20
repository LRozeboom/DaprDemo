using System.Collections.Concurrent;

namespace Demo04.Outbox.Worker.Orders;

/// <summary>
/// Demo scaffolding for the resiliency half of the demo: an order can ask for its first N
/// deliveries to fail, so the audience sees the outbox's at-least-once delivery meet demo 02's
/// retry policy. Orders placed without `failDeliveries` never fail.
/// </summary>
public sealed class OrderDeliveryPlan
{
    private readonly ConcurrentDictionary<Guid, Plan> _plans = new();

    public void Arm(Guid orderId, int plannedFailures)
    {
        if (plannedFailures > 0)
        {
            _plans[orderId] = new Plan(plannedFailures);
        }
    }

    public OrderDelivery Next(Guid orderId) =>
        _plans.TryGetValue(orderId, out var plan)
            ? new OrderDelivery(plan.NextAttempt(), plan.PlannedFailures)
            : new OrderDelivery(Attempt: 1, PlannedFailures: 0);

    private sealed class Plan(int plannedFailures)
    {
        private int _attempts;

        public int PlannedFailures { get; } = plannedFailures;

        public int NextAttempt() => Interlocked.Increment(ref _attempts);
    }
}

public readonly record struct OrderDelivery(int Attempt, int PlannedFailures)
{
    public bool ShouldFail => Attempt <= PlannedFailures;
}
