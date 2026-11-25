namespace Zeeway.Pedidos.Domain;

public sealed class PedidoItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Nome { get; private set; } = null!;
    public int Quantidade { get; private set; }
    public decimal PrecoUnitario { get; private set; }

    private PedidoItem() { }

    public PedidoItem(string nome, int quantidade, decimal precoUnitario)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("O Nome do Item é obrigatorio.", nameof(nome));
        if (quantidade <= 0) throw new ArgumentOutOfRangeException(nameof(quantidade));
        if (precoUnitario < 0) throw new ArgumentOutOfRangeException(nameof(precoUnitario));
        Nome = nome.Trim();
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
    }
}
