namespace Zeeway.Pedidos.Domain;

public sealed class PagamentoEventRecord
{
    public string EventId { get; private set; } = null!;
    public Guid PedidoId { get; private set; }
    public string Type { get; private set; } = null!;
    public DateTimeOffset ReceivedAt { get; private set; } = DateTimeOffset.UtcNow;

    private PagamentoEventRecord() { }

    public PagamentoEventRecord(string eventId, Guid pedidoId, string type)
    {
        EventId = eventId;
        PedidoId = pedidoId;
        Type = type;
    }
}
