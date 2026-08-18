using DaprDemos.SharedKernel.Messaging;
using DaprDemos.SharedKernel.Results;

namespace Demo03.StateStore.Worker.Counter.IncrementCounter;

public sealed class IncrementCounterCommandHandler(
    CounterStore counterStore) : ICommandHandler<IncrementCounterCommand, int>
{
    private const int MaxAttempts = 20;

    public async Task<Result<int>> HandleAsync(
        IncrementCounterCommand command,
        CancellationToken cancellationToken)
    {
        // Optimistic concurrency, for free from the state store: read the value together with its
        // ETag, then write back only if nobody has touched the key since. If someone has, the
        // store rejects the write and we re-read and try again rather than clobbering them.
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var (current, etag) = await counterStore.GetWithETagAsync(cancellationToken);

            if (await counterStore.TrySaveAsync(current + 1, etag, cancellationToken))
            {
                return current + 1;
            }
        }

        return CounterErrors.ConcurrencyConflict(CounterStore.Key);
    }
}
