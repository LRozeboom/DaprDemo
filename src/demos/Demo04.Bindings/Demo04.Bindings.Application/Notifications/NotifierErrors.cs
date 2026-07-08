using DaprDemos.SharedKernel.Results;

namespace Demo04.Bindings.Application.Notifications;

public static class NotifierErrors
{
    public static Error DeliveryFailed(string detail) =>
        new("Notifier.DeliveryFailed", $"The alert could not be delivered: {detail}");
}
