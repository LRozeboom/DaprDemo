using DaprDemos.SharedKernel.Results;

namespace Demo04.Bindings.Domain.Alerts;

// Error catalogs usually live in Application, but Alert.Create owns this validation and
// Domain cannot reference Application — so the catalog sits next to the factory instead.
public static class AlertErrors
{
    public static Error EmptyTitle() =>
        new("Alert.EmptyTitle", "An alert requires a non-empty title.");

    public static Error EmptyMessage() =>
        new("Alert.EmptyMessage", "An alert requires a non-empty message.");
}
