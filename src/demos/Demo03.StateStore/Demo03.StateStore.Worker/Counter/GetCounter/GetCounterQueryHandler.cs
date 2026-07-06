using DaprDemos.SharedKernel.Results;

namespace Demo03.StateStore.Worker.Counter.GetCounter;

public sealed class GetCounterQueryHandler(CounterStore counterStore) : IQueryHandler<GetCounterQuery, int>
{
    public async Task<Result<int>> HandleAsync(GetCounterQuery query, CancellationToken cancellationToken) =>
        await counterStore.GetAsync(cancellationToken);
}
