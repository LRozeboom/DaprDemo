using DaprDemos.SharedKernel.Messaging;
using DaprDemos.SharedKernel.Results;

namespace Demo03.StateStore.Worker.Counter.RunIncrements;

public sealed class CounterRunner(
    RunSignal runSignal,
    IServiceScopeFactory scopeFactory,
    ILogger<CounterRunner> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var command in runSignal.ReadAllAsync(stoppingToken))
        {
            using var scope = scopeFactory.CreateScope();
            var handler = scope.ServiceProvider
                .GetRequiredService<ICommandHandler<RunIncrementsCommand, Unit>>();

            try
            {
                await handler.HandleAsync(command, stoppingToken);
            }
            catch (Exception exception)
            {
                // Load-bearing on stage: an escaping exception (sidecar down, say) would end this
                // loop for good and every later /run would silently do nothing.
                logger.LogError(exception, "Run failed");
            }
        }
    }
}
