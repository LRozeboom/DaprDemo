using DaprDemos.SharedKernel.Results;

namespace Demo03.StateStore.Worker.Counter;

public static class CounterErrors
{
    public static Error ConcurrencyConflict(string key) =>
        new("Counter.ConcurrencyConflict", $"Could not update state key '{key}': ETag conflicts persisted after repeated retries.");
}
