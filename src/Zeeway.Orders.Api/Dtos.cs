namespace Zeeway.Orders.Api;

public sealed record CreateOrderItemDto(
    string Name,
    int Quantity,
    decimal UnitPrice);

public sealed record CreateOrderRequest(
    string CustomerEmail,
    IReadOnlyCollection<CreateOrderItemDto> Items);

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
