using Zeeway.Pedidos.Domain;

namespace Zeeway.Pedidos.Api.Mapping;

public static class PedidoApiMapper
{
    public static PedidoResponse ToResponse(Pedido pedido) => new(
        pedido.Id,
        pedido.ValorTotal,
        pedido.EmailCliente,
        ToResponseStatus(pedido.Status),
        pedido.PagamentoId,
        pedido.DataCreated,
        pedido.DataUpdated,
        pedido.Items.Select(item =>
            new PedidoItemResponse(item.Id, item.Nome, item.Quantidade, item.PrecoUnitario)).ToList());

    public static bool TryParseStatus(string status, out PedidoStatus parsed)
    {
        switch (status.Trim().ToLowerInvariant())
        {
            case "pending":
            case "pendente":
                parsed = PedidoStatus.Pendente;
                return true;
            case "paid":
            case "pago":
                parsed = PedidoStatus.Pago;
                return true;
            case "failed":
            case "falha":
                parsed = PedidoStatus.Falha;
                return true;
            default:
                parsed = default;
                return false;
        }
    }

    private static string ToResponseStatus(PedidoStatus status) => status switch
    {
        PedidoStatus.Pendente => "Pending",
        PedidoStatus.Pago => "Paid",
        PedidoStatus.Falha => "Failed",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown pedido status.")
    };
}
