using Dapr.Client;
using DaprDemos.Contracts.Messaging;

namespace Demo03.StateStore.Worker.Counter;

/// <summary>
/// Every state call the demo makes, in one place. Note what is absent: no Redis client, no
/// connection string, no serializer — just a component name and a key.
/// </summary>
public sealed class CounterStore(DaprClient daprClient)
{
    public const string Key = "demo-counter";

    public Task<(int Value, string ETag)> GetWithETagAsync(CancellationToken cancellationToken) =>
        daprClient.GetStateAndETagAsync<int>(Components.StateStore, Key, cancellationToken: cancellationToken);

    public Task SaveAsync(int value, CancellationToken cancellationToken) =>
        daprClient.SaveStateAsync(Components.StateStore, Key, value, cancellationToken: cancellationToken);

    public async Task<bool> TrySaveAsync(int value, string etag, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(etag))
        {
            // First write for the key: no ETag exists yet, so an unconditional save creates it.
            await daprClient.SaveStateAsync(Components.StateStore, Key, value, cancellationToken: cancellationToken);
            return true;
        }

        return await daprClient.TrySaveStateAsync(Components.StateStore, Key, value, etag, cancellationToken: cancellationToken);
    }
}
