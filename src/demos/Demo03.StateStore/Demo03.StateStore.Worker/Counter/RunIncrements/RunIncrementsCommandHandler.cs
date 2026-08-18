using DaprDemos.SharedKernel.Messaging;
using DaprDemos.SharedKernel.Results;
using Demo03.StateStore.Worker.Counter.IncrementCounter;

namespace Demo03.StateStore.Worker.Counter.RunIncrements;

/// <summary>
/// The loops run <see cref="Concurrency"/>-wide *within* one run, so a single worker already
/// races itself through the state store and the conflict count is never zero. Running both
/// workers then shows the same contention across processes.
/// </summary>
public sealed class RunIncrementsCommandHandler(
    ICommandHandler<IncrementCounterCommand, IncrementCounterResult> incrementHandler,
    ILogger<RunIncrementsCommandHandler> logger) : ICommandHandler<RunIncrementsCommand, Unit>
{
    public const int Concurrency = 4;

    // Sized by wall-clock, not by the total: a run has to still be going when you arm the second
    // worker, and switching browser tabs in the Scalar UI costs a few seconds. ~10 s of run gives
    // room to do that; anything near 1 s and worker A is always finished before B starts.
    public const int IterationsPerLoop = 500;
    public const int Iterations = Concurrency * IterationsPerLoop;

    public async Task<Result<Unit>> HandleAsync(
        RunIncrementsCommand command,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Starting {Iterations} ETag-guarded increments across {Concurrency} concurrent loops",
            Iterations,
            Concurrency);

        // Safe to share the handler across the loops: it and CounterStore are stateless, and
        // DaprClient is thread-safe.
        var loops = Enumerable
            .Range(0, Concurrency)
            .Select(_ => RunLoopAsync(cancellationToken));

        var results = await Task.WhenAll(loops);

        var failed = results.Sum(result => result.Failed);
        var conflicts = results.Sum(result => result.Conflicts);
        var lastObserved = results.Max(result => result.LastObserved);

        logger.LogInformation(
            "Run finished: {Succeeded} increments succeeded, {Failures} failed, {Conflicts} ETag conflicts detected and retried, last observed counter value {LastObserved}",
            Iterations - failed,
            failed,
            conflicts,
            lastObserved);

        return Unit.Value;
    }

    private async Task<LoopResult> RunLoopAsync(CancellationToken cancellationToken)
    {
        var failed = 0;
        var conflicts = 0;
        var lastObserved = 0;

        for (var i = 0; i < IterationsPerLoop; i++)
        {
            var result = await incrementHandler.HandleAsync(new IncrementCounterCommand(), cancellationToken);

            if (result.IsSuccess)
            {
                lastObserved = result.Value.Value;
                conflicts += result.Value.Attempts - 1;
            }
            else
            {
                failed++;
                logger.LogWarning("Increment failed: {Code} — {Message}", result.Error.Code, result.Error.Message);
            }

            // Paces the run so it stays readable on a projector rather than finishing in a blur.
            await Task.Delay(Random.Shared.Next(1, 11), cancellationToken);
        }

        return new LoopResult(failed, conflicts, lastObserved);
    }

    private readonly record struct LoopResult(int Failed, int Conflicts, int LastObserved);
}
