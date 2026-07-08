using DaprDemos.SharedKernel.Results;

namespace DaprDemos.SharedKernel.Messaging;

public interface IQueryHandler<in TQuery, TResponse>
{
    Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
