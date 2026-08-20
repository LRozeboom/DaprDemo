using System.Text.Json;
using Dapr.Client;
using DaprDemos.Contracts.Messaging;
using DaprDemos.Contracts.Messaging.Events;

namespace Demo04.Outbox.Worker.Orders;

public sealed class OrderStore(DaprClient daprClient)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static string KeyFor(Guid orderId) => $"order-{orderId}";

    /// <summary>Stores the order and hands Dapr the event to publish, as a single state transaction.</summary>
    public Task CommitAsync(OrderRecord order, OrderPlacedEvent orderPlaced, CancellationToken cancellationToken)
    {
        var key = KeyFor(order.Id);

        // Operation 1 — the row that actually gets stored.
        var writeOrder = new StateTransactionRequest(
            key,
            JsonSerializer.SerializeToUtf8Bytes(order, SerializerOptions),
            StateOperationType.Upsert);

        // Operation 2 — same key, marked `outbox.projection`. It is never written to the store: it
        // only tells Dapr what the published message should look like, which is how the event on
        // the topic can be narrower than the row in the database.
        var publishOrderPlaced = new StateTransactionRequest(
            key,
            JsonSerializer.SerializeToUtf8Bytes(orderPlaced, SerializerOptions),
            StateOperationType.Upsert,
            metadata: new Dictionary<string, string>
            {
                ["outbox.projection"] = "true",
                ["contentType"] = "application/json",
            });

        return daprClient.ExecuteStateTransactionAsync(
            Components.OutboxStore,
            [writeOrder, publishOrderPlaced],
            cancellationToken: cancellationToken);
    }

    public Task<OrderRecord?> GetAsync(Guid orderId, CancellationToken cancellationToken) =>
        daprClient.GetStateAsync<OrderRecord?>(
            Components.OutboxStore,
            KeyFor(orderId),
            cancellationToken: cancellationToken);
}
