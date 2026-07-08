using DaprDemos.SharedKernel.Messaging;
using DaprDemos.SharedKernel.Results;
using Demo04.Bindings.Application.Notifications;
using Demo04.Bindings.Domain.Alerts;

namespace Demo04.Bindings.Application.Alerts.RaiseAlert;

public sealed class RaiseAlertCommandHandler(
    INotifier notifier,
    TimeProvider timeProvider) : ICommandHandler<RaiseAlertCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(RaiseAlertCommand command, CancellationToken cancellationToken)
    {
        var alertResult = Alert.Create(command.Title, command.Message, timeProvider.GetUtcNow());

        return await alertResult.MatchAsync(
            alert => notifier.NotifyAsync(alert, cancellationToken),
            error => Task.FromResult<Result<Unit>>(error));
    }
}
