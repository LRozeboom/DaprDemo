using DaprDemos.SharedKernel.Results;

namespace Demo03.StateStore.Worker;

// Deliberately re-declared per service instead of shared: keeps every demo free of mediator frameworks.
public interface ICommandHandler<in TCommand, TResponse>
{
    Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
