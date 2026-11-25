namespace Zeeway.Pedidos.Domain;

public sealed class IdempotencyRecord
{
    public string Key { get; private set; } = null!;
    public Guid PedidoId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private IdempotencyRecord() { }

    public IdempotencyRecord(string key, Guid pedidoId)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Idempotency key is required.", nameof(key));
        Key = key.Trim();
        PedidoId = pedidoId;
    }
}
