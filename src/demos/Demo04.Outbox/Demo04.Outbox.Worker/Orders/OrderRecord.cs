namespace Demo04.Outbox.Worker.Orders;

public sealed record OrderRecord(
    Guid Id,
    string Customer,
    decimal Amount,
    DateTimeOffset PlacedAt,
    string Status)
{
    public const string PlacedStatus = "Placed";
}
