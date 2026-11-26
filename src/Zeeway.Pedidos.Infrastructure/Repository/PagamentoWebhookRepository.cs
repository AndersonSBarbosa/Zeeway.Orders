using Microsoft.EntityFrameworkCore;
using Zeeway.Pedidos.Domain;
using Zeeway.Pedidos.Infrastructure.db;
using Zeeway.Pedidos.Infrastructure.Interface;

namespace Zeeway.Pedidos.Infrastructure.Repository;

public sealed class PagamentoWebhookRepository : IPagamentoWebhookRepository
{
    private readonly PedidosDbContext _db;

    public PagamentoWebhookRepository(PedidosDbContext db) => _db = db;

    public Task<bool> EventExistsAsync(string eventId, CancellationToken cancellationToken)
    {
        return _db.EventosPagamento
            .AsNoTracking()
            .AnyAsync(x => x.EventoId == eventId, cancellationToken);
    }

    public Task<Pedido?> GetPedidoAsync(Guid pedidoId, CancellationToken cancellationToken)
    {
        return _db.Pedidos
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == pedidoId, cancellationToken);
    }

    public async Task<bool> SaveEventAsync(PagamentoEventRecord evento,CancellationToken cancellationToken)
    {
        _db.EventosPagamento.Add(evento);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            if (await EventExistsAsync(evento.EventoId, cancellationToken))
                return false;
            throw;
        }
    }
}
