using DaprDemos.SharedKernel.Results;
using Demo04.Bindings.Domain.Alerts;

namespace Demo04.Bindings.Application.Notifications;

public interface INotifier
{
    Task<Result<Unit>> NotifyAsync(Alert alert, CancellationToken cancellationToken);
}
