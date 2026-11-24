using System.ComponentModel.DataAnnotations;

namespace Zeeway.Orders.Api;

public sealed record CreateOrderItemDto(
    [property: Required] string Name,
    [property: Range(1, int.MaxValue)] int Quantity,
    [property: Range(typeof(decimal), "0.01", "79228162514264337593543950335")] decimal UnitPrice);

public sealed record CreateOrderRequest(
    [property: Required] string CustomerEmail,
    [property: Required, MinLength(1)] IReadOnlyCollection<CreateOrderItemDto> Items);

public sealed record OrderResponse(
    Guid Id,
    decimal TotalAmount,
    string CustomerEmail,
    string Status,
    string? PaymentId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyCollection<OrderItemResponse> Items);

public sealed record OrderItemResponse(Guid Id, string Name, int Quantity, decimal UnitPrice);

public sealed record PagedOrdersResponse(IReadOnlyCollection<OrderResponse> Items, int Page, int PageSize, int Total);
