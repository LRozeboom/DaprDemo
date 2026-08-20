namespace Demo04.Outbox.Worker.Orders;

/// <summary>What the state store keeps. Wider than the event that goes out on the topic.</summary>
public sealed record OrderRecord(
    Guid Id,
    string Customer,
    decimal Amount,
    DateTimeOffset PlacedAt,
    string Status)
{
    public const string PlacedStatus = "Placed";
}
