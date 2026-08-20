using System.Text.Json;
using Dapr.Client;
using DaprDemos.Contracts.Messaging;
using DaprDemos.Contracts.Messaging.Events;

namespace Demo04.Outbox.Worker.Orders;

/// <summary>
/// The whole transactional outbox, in one method. The app writes state; it never publishes. Dapr
/// writes an outbox marker row inside the same database transaction and publishes the event only
/// once that transaction has committed — so "row stored" and "event published" cannot come apart.
/// </summary>
public sealed class OrderStore(DaprClient daprClient)
{
    /// <summary>
    /// An ETag the row cannot possibly have (it is Postgres' `xmin`, which starts far higher).
    /// Passing it makes the store reject the write the way a genuine concurrent update would —
    /// demo 03's optimistic concurrency, used here to force a rollback on demand.
    /// </summary>
    private const string StaleETag = "1";

    // camelCase on the wire, which is what DaprClient writes and what ASP.NET model binding reads.
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static string KeyFor(Guid orderId) => $"order-{orderId}";

    /// <summary>
    /// Stores the order and hands Dapr the event to publish, as a single state transaction.
    /// Throws <see cref="DaprException"/> when the store rejects the transaction.
    /// </summary>
    public Task CommitAsync(
        OrderRecord order,
        OrderPlacedEvent orderPlaced,
        bool forceConflict,
        CancellationToken cancellationToken)
    {
        var key = KeyFor(order.Id);

        // Operation 1 — the row. With `forceConflict` it carries a stale ETag, so the store rejects
        // it and the whole transaction (marker row included) rolls back.
        var writeOrder = new StateTransactionRequest(
            key,
            JsonSerializer.SerializeToUtf8Bytes(order, SerializerOptions),
            StateOperationType.Upsert,
            etag: forceConflict ? StaleETag : null);

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
                // Load-bearing: without it Dapr publishes the payload as text/plain and subscribers
                // receive a JSON *string* instead of a JSON object.
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
