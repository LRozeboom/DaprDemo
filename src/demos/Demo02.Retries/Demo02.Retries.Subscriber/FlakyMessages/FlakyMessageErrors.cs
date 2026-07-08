using DaprDemos.SharedKernel.Results;

namespace Demo02.Retries.Subscriber.FlakyMessages;

public static class FlakyMessageErrors
{
    public static Error SimulatedFailure(Guid id) =>
        new("FlakyMessage.SimulatedFailure", $"Simulated failure while processing message {id}.");
}
