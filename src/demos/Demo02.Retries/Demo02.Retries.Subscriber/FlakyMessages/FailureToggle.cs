namespace Demo02.Retries.Subscriber.FlakyMessages;

/// <summary>In-memory switch that makes the next N deliveries fail, so Dapr's redelivery is observable live.</summary>
public sealed class FailureToggle
{
    private int _remainingFailures;

    public int Remaining => Volatile.Read(ref _remainingFailures);

    public void FailNext(int count) => Interlocked.Exchange(ref _remainingFailures, count);

    public bool TryConsumeFailure()
    {
        while (true)
        {
            var current = Volatile.Read(ref _remainingFailures);
            if (current <= 0)
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref _remainingFailures, current - 1, current) == current)
            {
                return true;
            }
        }
    }
}
