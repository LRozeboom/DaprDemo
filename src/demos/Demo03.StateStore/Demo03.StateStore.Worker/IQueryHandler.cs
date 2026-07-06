using DaprDemos.SharedKernel.Results;

namespace Demo03.StateStore.Worker;

public interface IQueryHandler<in TQuery, TResponse>
{
    Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
