using DaprDemos.SharedKernel.Results;

namespace Demo04.Bindings.Domain.Alerts;

public static class AlertErrors
{
    public static Error EmptyTitle() =>
        new("Alert.EmptyTitle", "An alert requires a non-empty title.");

    public static Error EmptyMessage() =>
        new("Alert.EmptyMessage", "An alert requires a non-empty message.");
}
