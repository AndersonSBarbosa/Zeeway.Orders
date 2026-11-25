namespace Zeeway.Pedidos.Domain;

public sealed class PagamentoEventRecord
{
    public string EventoId { get; private set; } = null!;
    public Guid PedidoId { get; private set; }
    public string Tipo { get; private set; } = null!;
    public DateTimeOffset DataRecebimento { get; private set; } = DateTimeOffset.UtcNow;

    private PagamentoEventRecord() { }

    public PagamentoEventRecord(string eventoId, Guid pedidoId, string tipo, DateTimeOffset dataRecebimento)
    {
        EventoId=eventoId;
        PedidoId=pedidoId;
        Tipo=tipo;
        DataRecebimento=dataRecebimento;
    }
}
