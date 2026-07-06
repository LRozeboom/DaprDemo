using DaprDemos.SharedKernel.Results;

namespace Demo02.Retries.Subscriber;

// Deliberately re-declared per service instead of shared: keeps every demo free of mediator frameworks.
public interface ICommandHandler<in TCommand, TResponse>
{
    Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
