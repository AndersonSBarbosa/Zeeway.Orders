using System.Text.Json.Serialization;

namespace Zeeway.Pedidos.Api;

public sealed record CreatePedidoItemDto(
    string Name,
    int Quantity,
    decimal UnitPrice);

public sealed record CreatePedidoRequest(
    string CustomerEmail,
    IReadOnlyCollection<CreatePedidoItemDto> Items);

public sealed record PedidoResponse(
    Guid Id,
    decimal TotalAmount,
    string CustomerEmail,
    string Status,
    [property: JsonPropertyName("paymentId")] string? PagamentoId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyCollection<PedidoItemResponse> Items);

public sealed record PedidoItemResponse(Guid Id, string Name, int Quantity, decimal UnitPrice);

public sealed record PagedPedidosResponse(
    IReadOnlyCollection<PedidoResponse> Items,
    int Page,
    int PageSize,
    int Total);
