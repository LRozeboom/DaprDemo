using DaprDemos.SharedKernel.Messaging;
using DaprDemos.SharedKernel.Results;

namespace Demo03.StateStore.Worker.Counter.IncrementCounter;

public sealed class IncrementCounterCommandHandler(
    CounterStore counterStore,
    CounterOptions options) : ICommandHandler<IncrementCounterCommand, int>
{
    private const int MaxAttempts = 20;

    public async Task<Result<int>> HandleAsync(IncrementCounterCommand command, CancellationToken cancellationToken)
    {
        if (!options.UseETags)
        {
            // Plain read-modify-write: two workers doing this concurrently WILL lose updates.
            var current = await counterStore.GetAsync(cancellationToken);
            var next = current + 1;
            await counterStore.SaveAsync(next, cancellationToken);
            return next;
        }

        // Bounded ETag retry loop with jitter;
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var (current, etag) = await counterStore.GetWithETagAsync(cancellationToken);
            var next = current + 1;

            if (await counterStore.TrySaveAsync(next, etag, cancellationToken))
            {
                return next;
            }

            await Task.Delay(Random.Shared.Next(1, 15), cancellationToken);
        }

        return CounterErrors.ConcurrencyConflict(CounterStore.Key);
    }
}
