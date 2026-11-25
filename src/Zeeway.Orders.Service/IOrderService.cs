using Zeeway.Orders.Domain;

namespace Zeeway.Orders.Service;

public interface IOrderService
{
    Task<Order> CreateAsync(CreateOrderCommand command, CancellationToken cancellationToken);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedOrders> ListAsync(OrderStatus? status, int page, int pageSize, CancellationToken cancellationToken);
}

public sealed record CreateOrderCommand(
    string CustomerEmail,
    IReadOnlyCollection<CreateOrderItemCommand> Items,
    string IdempotencyKey);

public sealed record CreateOrderItemCommand(string Name, int Quantity, decimal UnitPrice);

public sealed record PagedOrders(IReadOnlyCollection<Order> Items, int Page, int PageSize, int Total);
