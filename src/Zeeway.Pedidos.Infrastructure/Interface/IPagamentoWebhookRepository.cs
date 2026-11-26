using Zeeway.Pedidos.Domain;

namespace Zeeway.Pedidos.Infrastructure.Interface;

public interface IPagamentoWebhookRepository
{
    Task<bool> EventExistsAsync(string eventId, CancellationToken cancellationToken);
    Task<Pedido?> GetPedidoAsync(Guid pedidoId, CancellationToken cancellationToken);
    Task<bool> SaveEventAsync(PagamentoEventRecord evento, CancellationToken cancellationToken);
}
