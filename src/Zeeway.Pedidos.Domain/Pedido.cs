namespace Zeeway.Pedidos.Domain;

public sealed class Pedido
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public decimal TotalAmount { get; private set; }
    public string CustomerEmail { get; private set; } = null!;
    public PedidoStatus Status { get; private set; } = PedidoStatus.Pending;
    public string? PagamentoId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    private readonly List<PedidoItem> _items = new();
    public IReadOnlyCollection<PedidoItem> Items => _items.AsReadOnly();

    private Pedido() { }

    public Pedido(string customerEmail, IEnumerable<PedidoItem> items)
    {
        if (string.IsNullOrWhiteSpace(customerEmail)) throw new ArgumentException("Customer email is required.", nameof(customerEmail));
        var itemList = items?.ToList() ?? throw new ArgumentNullException(nameof(items));
        if (itemList.Count == 0) throw new ArgumentException("At least one item is required.", nameof(items));

        CustomerEmail = customerEmail.Trim();
        _items.AddRange(itemList);
        TotalAmount = itemList.Sum(item => item.UnitPrice * item.Quantity);
    }

    public void SetPagamentoId(string pagamentoId)
    {
        if (string.IsNullOrWhiteSpace(pagamentoId)) throw new ArgumentException("Pagamento id is required.", nameof(pagamentoId));
        PagamentoId = pagamentoId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool ApplyPagamentoStatus(PedidoStatus targetStatus)
    {
        if (targetStatus == Status) return false;
        if (Status != PedidoStatus.Pending)
            throw new InvalidOperationException($"Cannot transition from {Status} to {targetStatus}.");
        if (targetStatus is not (PedidoStatus.Paid or PedidoStatus.Failed))
            throw new InvalidOperationException($"Invalid pagamento status: {targetStatus}.");

        Status = targetStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
        return true;
    }
}
