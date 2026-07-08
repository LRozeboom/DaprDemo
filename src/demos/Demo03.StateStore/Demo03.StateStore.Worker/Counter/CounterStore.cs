using Dapr.Client;
using DaprDemos.Contracts.Messaging;

namespace Demo03.StateStore.Worker.Counter;

public sealed class CounterStore(DaprClient daprClient)
{
    public const string Key = "demo-counter";

    public async Task<int> GetAsync(CancellationToken cancellationToken) =>
        await daprClient.GetStateAsync<int>(Components.StateStore, Key, cancellationToken: cancellationToken);

    public Task SaveAsync(int value, CancellationToken cancellationToken) =>
        daprClient.SaveStateAsync(Components.StateStore, Key, value, cancellationToken: cancellationToken);

    public Task<(int Value, string ETag)> GetWithETagAsync(CancellationToken cancellationToken) =>
        daprClient.GetStateAndETagAsync<int>(Components.StateStore, Key, cancellationToken: cancellationToken);

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
