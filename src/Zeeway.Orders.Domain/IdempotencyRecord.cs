namespace Zeeway.Orders.Domain;

public sealed class IdempotencyRecord
{
    public string Key { get; private set; } = null!;
    public Guid OrderId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private IdempotencyRecord() { }

    public IdempotencyRecord(string key, Guid orderId)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Idempotency key is required.", nameof(key));
        Key = key.Trim();
        OrderId = orderId;
    }
}
