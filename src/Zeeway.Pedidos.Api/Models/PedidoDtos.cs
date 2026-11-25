using System.Text.Json.Serialization;

namespace Zeeway.Pedidos.Api;

public sealed record CreatePedidoItemDto(
    [property: JsonPropertyName("name")] string Nome,
    [property: JsonPropertyName("quantity")] int Quantidade,
    [property: JsonPropertyName("unitPrice")] decimal PrecoUnitario);

public sealed record CreatePedidoRequest(
    [property: JsonPropertyName("customerEmail")] string EmailCliente,
    IReadOnlyCollection<CreatePedidoItemDto> Items);

public sealed record PedidoResponse(
    Guid Id,
    [property: JsonPropertyName("totalAmount")] decimal ValorTotal,
    [property: JsonPropertyName("customerEmail")] string EmailCliente,
    string Status,
    [property: JsonPropertyName("paymentId")] string? PagamentoId,
    [property: JsonPropertyName("createdAt")] DateTimeOffset DataCreated,
    [property: JsonPropertyName("updatedAt")] DateTimeOffset DataUpdated,
    IReadOnlyCollection<PedidoItemResponse> Items);

public sealed record PedidoItemResponse(
    Guid Id,
    [property: JsonPropertyName("name")] string Nome,
    [property: JsonPropertyName("quantity")] int Quantidade,
    [property: JsonPropertyName("unitPrice")] decimal PrecoUnitario);

public sealed record PagedPedidosResponse(
    IReadOnlyCollection<PedidoResponse> Items,
    int Page,
    int PageSize,
    int Total);
