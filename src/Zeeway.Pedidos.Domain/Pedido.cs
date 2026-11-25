namespace Zeeway.Pedidos.Domain;

public sealed class Pedido : Base
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public decimal ValorTotal { get; private set; }
    public string EmailCliente { get; private set; } = null!;
    public PedidoStatus Status { get; private set; } = PedidoStatus.Pendente;
    public string? PagamentoId { get; private set; }

    private readonly List<PedidoItem> _items = new();
    public IReadOnlyCollection<PedidoItem> Items => _items.AsReadOnly();

    private Pedido() { }

    public Pedido(string customerEmail, IEnumerable<PedidoItem> items)
    {
        if (string.IsNullOrWhiteSpace(customerEmail)) throw new ArgumentException("email do cliente é Obrigatorio.", nameof(customerEmail));
        var itemList = items?.ToList() ?? throw new ArgumentNullException(nameof(items));
        if (itemList.Count == 0) throw new ArgumentException("É obrigatório incluir, pelo menos, um item", nameof(items));

        EmailCliente = customerEmail.Trim();
        _items.AddRange(itemList);
        ValorTotal = itemList.Sum(item => item.PrecoUnitario * item.Quantidade);
    }

    public void SetPagamentoId(string pagamentoId)
    {
        if (string.IsNullOrWhiteSpace(pagamentoId)) throw new ArgumentException("Pagamento id é obrigatorio.", nameof(pagamentoId));
        PagamentoId = pagamentoId;
        DataUpdated = DateTimeOffset.UtcNow;
    }

    public bool ApplyPagamentoStatus(PedidoStatus targetStatus)
    {
        if (targetStatus == Status) return false;
        if (Status != PedidoStatus.Pendente)
            throw new InvalidOperationException($"Cannot transition from {Status} to {targetStatus}.");
        if (targetStatus is not (PedidoStatus.Pago or PedidoStatus.Falha))
            throw new InvalidOperationException($"Invalid pagamento status: {targetStatus}.");

        Status = targetStatus;
        DataUpdated = DateTimeOffset.UtcNow;
        return true;
    }
}
