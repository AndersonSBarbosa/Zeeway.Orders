namespace Zeeway.Orders.Domain;

public sealed class PaymentEventRecord
{
    public string EventId { get; private set; } = null!;
    public Guid OrderId { get; private set; }
    public string Type { get; private set; } = null!;
    public DateTimeOffset ReceivedAt { get; private set; } = DateTimeOffset.UtcNow;

    private PaymentEventRecord() { }

    public PaymentEventRecord(string eventId, Guid orderId, string type)
    {
        EventId = eventId;
        OrderId = orderId;
        Type = type;
    }
}
