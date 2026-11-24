namespace Zeeway.Orders.Domain;

public sealed class Order
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public decimal TotalAmount { get; private set; }
    public string CustomerEmail { get; private set; } = null!;
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public string? PaymentId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { }

    public Order(string customerEmail, IEnumerable<OrderItem> items)
    {
        if (string.IsNullOrWhiteSpace(customerEmail)) throw new ArgumentException("Customer email is required.", nameof(customerEmail));
        var itemList = items?.ToList() ?? throw new ArgumentNullException(nameof(items));
        if (itemList.Count == 0) throw new ArgumentException("At least one item is required.", nameof(items));

        CustomerEmail = customerEmail.Trim();
        _items.AddRange(itemList);
        TotalAmount = itemList.Sum(item => item.UnitPrice * item.Quantity);
    }

    public void SetPaymentId(string paymentId)
    {
        if (string.IsNullOrWhiteSpace(paymentId)) throw new ArgumentException("Payment id is required.", nameof(paymentId));
        PaymentId = paymentId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool ApplyPaymentStatus(OrderStatus targetStatus)
    {
        if (targetStatus == Status) return false;
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Cannot transition from {Status} to {targetStatus}.");
        if (targetStatus is not (OrderStatus.Paid or OrderStatus.Failed))
            throw new InvalidOperationException($"Invalid payment status: {targetStatus}.");

        Status = targetStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
        return true;
    }
}
