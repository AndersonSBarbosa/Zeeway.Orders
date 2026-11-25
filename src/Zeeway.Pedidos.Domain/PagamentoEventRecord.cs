namespace Zeeway.Pedidos.Domain;

public sealed class PagamentoEventRecord
{
    public string EventId { get; private set; } = null!;
    public Guid PedidoId { get; private set; }
    public string Tipo { get; private set; } = null!;
    public DateTimeOffset DataRecebimento { get; private set; } = DateTimeOffset.UtcNow;

    private PagamentoEventRecord() { }

    public PagamentoEventRecord(string eventId, Guid pedidoId, string tipo, DateTimeOffset dataRecebimento)
    {
        EventId=eventId;
        PedidoId=pedidoId;
        Tipo=tipo;
        DataRecebimento=dataRecebimento;
    }
}
