using DaprDemos.SharedKernel.Messaging;
using Demo03.StateStore.Worker.Counter.IncrementCounter;

namespace Demo03.StateStore.Worker.Counter;

public sealed class CounterRunner(
    RunSignal runSignal,
    IServiceScopeFactory scopeFactory,
    CounterOptions options,
    ILogger<CounterRunner> logger) : BackgroundService
{
    public const int Iterations = 200;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var _ in runSignal.WaitForRunsAsync(stoppingToken))
        {
            logger.LogInformation(
                "Starting {Iterations} read-modify-write increments (ETags: {UseETags})",
                Iterations,
                options.UseETags);

            using var scope = scopeFactory.CreateScope();
            var handler = scope.ServiceProvider
                .GetRequiredService<ICommandHandler<IncrementCounterCommand, int>>();

            var failures = 0;
            var lastObserved = 0;

            for (var i = 0; i < Iterations; i++)
            {
                var result = await handler.HandleAsync(new IncrementCounterCommand(), stoppingToken);

                if (result.IsSuccess)
                {
                    lastObserved = result.Value;
                }
                else
                {
                    failures++;
                    logger.LogWarning("Increment failed: {Code} — {Message}", result.Error.Code, result.Error.Message);
                }

                await Task.Delay(Random.Shared.Next(1, 11), stoppingToken);
            }

            logger.LogInformation(
                "Run finished: {Succeeded} increments succeeded, {Failures} failed, last observed counter value {LastObserved}",
                Iterations - failures,
                failures,
                lastObserved);
        }
    }
}
