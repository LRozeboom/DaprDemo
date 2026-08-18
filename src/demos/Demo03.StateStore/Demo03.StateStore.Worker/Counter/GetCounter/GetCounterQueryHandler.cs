using DaprDemos.SharedKernel.Messaging;
using DaprDemos.SharedKernel.Results;

namespace Demo03.StateStore.Worker.Counter.GetCounter;

public sealed class GetCounterQueryHandler(CounterStore counterStore) : IQueryHandler<GetCounterQuery, CounterState>
{
    public async Task<Result<CounterState>> HandleAsync(GetCounterQuery query, CancellationToken cancellationToken)
    {
        // Reading the ETag alongside the value costs nothing extra and makes the thing the
        // increment is guarding against visible: it changes on every write.
        var (value, etag) = await counterStore.GetWithETagAsync(cancellationToken);

        return new CounterState(value, etag);
    }
}
