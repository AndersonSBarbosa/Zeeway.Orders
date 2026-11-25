namespace Zeeway.Pedidos.Domain;

public sealed class IdempotencyRecord :Base
{
    public string Key { get; private set; } = null!;
    public Guid PedidoId { get; private set; }

    private IdempotencyRecord() { }

    public IdempotencyRecord(string key, Guid pedidoId)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("A chave é obrigatória.", nameof(key));
        Key = key.Trim();
        PedidoId = pedidoId;
    }
}
