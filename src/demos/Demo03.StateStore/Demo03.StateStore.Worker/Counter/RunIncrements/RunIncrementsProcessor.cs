using DaprDemos.SharedKernel.Messaging;
using DaprDemos.SharedKernel.Results;

namespace Demo03.StateStore.Worker.Counter.RunIncrements;

/// <summary>Drains <see cref="RunSignal"/> and hands each queued run to the command handler.</summary>
public sealed class RunIncrementsProcessor(
    RunSignal runSignal,
    IServiceScopeFactory scopeFactory,
    ILogger<RunIncrementsProcessor> logger) : BackgroundService
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
                var result = await handler.HandleAsync(command, stoppingToken);

                if (result.IsFailure)
                {
                    logger.LogWarning("Run failed: {Code} — {Message}", result.Error.Code, result.Error.Message);
                }
            }
            catch (Exception exception)
            {
                // Load-bearing on stage: an escaping exception (sidecar down, say) would end this
                // loop for good and every later /run would silently do nothing.
                logger.LogError(exception, "Run threw");
            }
        }
    }
}
