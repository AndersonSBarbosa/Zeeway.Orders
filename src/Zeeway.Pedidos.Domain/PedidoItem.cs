namespace Zeeway.Pedidos.Domain;

public sealed class PedidoItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = null!;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    private PedidoItem() { }

    public PedidoItem(string name, int quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Item name is required.", nameof(name));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice));
        Name = name.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
