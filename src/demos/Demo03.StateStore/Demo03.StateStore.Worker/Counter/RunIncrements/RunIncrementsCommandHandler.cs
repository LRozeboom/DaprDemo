using DaprDemos.SharedKernel.Messaging;
using DaprDemos.SharedKernel.Results;
using Demo03.StateStore.Worker.Counter.IncrementCounter;

namespace Demo03.StateStore.Worker.Counter.RunIncrements;

public sealed class RunIncrementsCommandHandler(
    ICommandHandler<IncrementCounterCommand, int> incrementHandler,
    CounterOptions options,
    ILogger<RunIncrementsCommandHandler> logger) : ICommandHandler<RunIncrementsCommand, RunSummary>
{
    public const int Iterations = 200;

    public async Task<Result<RunSummary>> HandleAsync(
        RunIncrementsCommand command,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Starting {Iterations} read-modify-write increments (ETags: {UseETags})",
            Iterations,
            options.UseETags);

        var failures = 0;
        var lastObserved = 0;

        for (var i = 0; i < Iterations; i++)
        {
            var result = await incrementHandler.HandleAsync(new IncrementCounterCommand(), cancellationToken);

            if (result.IsSuccess)
            {
                lastObserved = result.Value;
            }
            else
            {
                failures++;
                logger.LogWarning("Increment failed: {Code} — {Message}", result.Error.Code, result.Error.Message);
            }

            // Small jitter between iterations so the two workers reliably interleave — without it
            // the lost-update effect in the USE_ETAGS=false run is far less consistent.
            await Task.Delay(Random.Shared.Next(1, 11), cancellationToken);
        }

        logger.LogInformation(
            "Run finished: {Succeeded} increments succeeded, {Failures} failed, last observed counter value {LastObserved}",
            Iterations - failures,
            failures,
            lastObserved);

        return new RunSummary(Iterations, Iterations - failures, failures, lastObserved);
    }
}
