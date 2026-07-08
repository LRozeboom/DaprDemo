using DaprDemos.SharedKernel.Results;

namespace DaprDemos.SharedKernel.Messaging;

public interface ICommandHandler<in TCommand, TResponse>
{
    Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
