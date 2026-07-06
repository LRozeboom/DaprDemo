using DaprDemos.SharedKernel.Results;

namespace Demo01.PubSub.Publisher;

// Deliberately re-declared per service instead of shared: keeps every demo free of mediator frameworks.
public interface ICommandHandler<in TCommand, TResponse>
{
    Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
