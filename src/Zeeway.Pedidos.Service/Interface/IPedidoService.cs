using Zeeway.Pedidos.Domain;

namespace Zeeway.Pedidos.Service.Interface;

public interface IPedidoService
{
    Task<Pedido> CreateAsync(CreatePedidoCommand command, CancellationToken cancellationToken);
    Task<Pedido?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedPedidos> ListAsync(PedidoStatus? status, int page, int pageSize, CancellationToken cancellationToken);
}

public sealed record CreatePedidoCommand(
    string CustomerEmail,
    IReadOnlyCollection<CreatePedidoItemCommand> Items,
    string IdempotencyKey);

public sealed record CreatePedidoItemCommand(string Name, int Quantity, decimal UnitPrice);

public sealed record PagedPedidos(IReadOnlyCollection<Pedido> Items, int Page, int PageSize, int Total);
