namespace DaprDemos.Contracts.Messaging.Events;

/// <summary>
/// What subscribers see. Deliberately narrower than the row demo 04 stores: the outbox projection
/// publishes this shape while the state store keeps the full order.
/// </summary>
public sealed record OrderPlacedEvent(Guid Id, string Customer, decimal Amount, DateTimeOffset PlacedAt);
