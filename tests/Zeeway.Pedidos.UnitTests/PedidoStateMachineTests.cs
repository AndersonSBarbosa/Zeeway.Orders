using Zeeway.Pedidos.Domain;

namespace Zeeway.Pedidos.UnitTests;

public sealed class PedidoStateMachineTests
{
    [Fact]
    public void Pending_can_transition_to_paid()
    {
        var pedido = CriarPedido();
        Assert.True(pedido.ApplyPagamentoStatus(PedidoStatus.Paid));
        Assert.Equal(PedidoStatus.Paid, pedido.Status);
    }

    [Fact]
    public void Pending_can_transition_to_failed()
    {
        var pedido = CriarPedido();
        Assert.True(pedido.ApplyPagamentoStatus(PedidoStatus.Failed));
        Assert.Equal(PedidoStatus.Failed, pedido.Status);
    }

    [Fact]
    public void Final_status_cannot_change()
    {
        var pedido = CriarPedido();
        pedido.ApplyPagamentoStatus(PedidoStatus.Paid);
        Assert.Throws<InvalidOperationException>(() => pedido.ApplyPagamentoStatus(PedidoStatus.Failed));
    }

    [Fact]
    public void Duplicate_final_event_is_ignored()
    {
        var pedido = CriarPedido();
        pedido.ApplyPagamentoStatus(PedidoStatus.Paid);
        Assert.False(pedido.ApplyPagamentoStatus(PedidoStatus.Paid));
    }

    [Fact]
    public void Idempotency_record_binds_key_to_one_pedido()
    {
        var pedidoId = Guid.NewGuid();
        var record = new IdempotencyRecord("key-123", pedidoId);
        Assert.Equal("key-123", record.Key);
        Assert.Equal(pedidoId, record.PedidoId);
    }

    private static Pedido CriarPedido() =>
        new("buyer@example.com", new[] { new PedidoItem("Coffee", 2, 12.50m) });
}
