using DaprDemos.SharedKernel.Messaging;
using DaprDemos.SharedKernel.Results;

namespace Demo03.StateStore.Worker.Counter.IncrementCounter;

public sealed class IncrementCounterCommandHandler(
    CounterStore counterStore) : ICommandHandler<IncrementCounterCommand, IncrementCounterResult>
{
    private const int MaxAttempts = 20;

    public async Task<Result<IncrementCounterResult>> HandleAsync(
        IncrementCounterCommand command,
        CancellationToken cancellationToken)
    {
        // Optimistic concurrency: read the value together with its ETag, then write back only if
        // nobody has touched the key since. Losing that race is an expected outcome, not an error —
        // the write is rejected, and this loop re-reads and tries again with jitter.
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var (current, etag) = await counterStore.GetWithETagAsync(cancellationToken);
            var next = current + 1;

            if (await counterStore.TrySaveAsync(next, etag, cancellationToken))
            {
                return new IncrementCounterResult(next, attempt);
            }

            await Task.Delay(Random.Shared.Next(1, 15), cancellationToken);
        }

        return CounterErrors.ConcurrencyConflict(CounterStore.Key);
    }
}
