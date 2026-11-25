using System.Text.Json.Serialization;

namespace Zeeway.Pedidos.Infrastructure.Model;

public sealed record CreatePagamentoRequest(
    [property: JsonPropertyName("orderId")] Guid PedidoId,
    decimal Valor,
    string EmailCliente);

public sealed record CreatePagamentoResponse(
    [property: JsonPropertyName("paymentId")] string PagamentoId,
    string Status);
