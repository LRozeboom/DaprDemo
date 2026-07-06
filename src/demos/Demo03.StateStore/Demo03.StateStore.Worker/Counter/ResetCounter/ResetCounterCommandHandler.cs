using DaprDemos.SharedKernel.Results;

namespace Demo03.StateStore.Worker.Counter.ResetCounter;

public sealed class ResetCounterCommandHandler(CounterStore counterStore) : ICommandHandler<ResetCounterCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(ResetCounterCommand command, CancellationToken cancellationToken)
    {
        await counterStore.SaveAsync(0, cancellationToken);
        return Unit.Value;
    }
}
