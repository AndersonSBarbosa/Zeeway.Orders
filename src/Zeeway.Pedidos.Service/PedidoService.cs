using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zeeway.Pedidos.Domain;
using Zeeway.Pedidos.Infrastructure.db;
using Zeeway.Pedidos.Infrastructure.Interface;
using Zeeway.Pedidos.Infrastructure.Model;
using Zeeway.Pedidos.Service.Interface;

namespace Zeeway.Pedidos.Service;

public sealed class PedidoService : IPedidoService
{
    private readonly PedidosDbContext _db;
    private readonly IPagamentoGatewayClient _gateway;
    private readonly ILogger<PedidoService> _logger;

    public PedidoService(
        PedidosDbContext db,
        IPagamentoGatewayClient gateway,
        ILogger<PedidoService> logger)
    {
        _db = db;
        _gateway = gateway;
        _logger = logger;
    }

    public async Task<Pedido> CreateAsync(CreatePedidoCommand command, CancellationToken cancellationToken)
    {
        var existing = await _db.IdempotencyRecords.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Key == command.IdempotencyKey, cancellationToken);
        if (existing is not null)
            return await GetExistingPedidoAsync(existing.PedidoId, cancellationToken);

        var pedido = new Pedido(
            command.CustomerEmail,
            command.Items.Select(x => new PedidoItem(x.Name, x.Quantity, x.UnitPrice)));
        await _db.Pedidos.AddAsync(pedido, cancellationToken);
        await _db.IdempotencyRecords.AddAsync(
            new IdempotencyRecord(command.IdempotencyKey, pedido.Id),
            cancellationToken);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            var winner = await _db.IdempotencyRecords.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Key == command.IdempotencyKey, cancellationToken);
            if (winner is null) throw;
            return await GetExistingPedidoAsync(winner.PedidoId, cancellationToken);
        }

        CreatePagamentoResponse pagamento;
        try
        {
            pagamento = await _gateway.CreatePagamentoAsync(
                new CreatePagamentoRequest(pedido.Id, pedido.ValorTotal, pedido.EmailCliente),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao criar pagamento para o pedido {PedidoId}", pedido.Id);
            return pedido;
        }

        pedido.SetPagamentoId(pagamento.PagamentoId);
        await _db.SaveChangesAsync(cancellationToken);
        return pedido;
    }

    public Task<Pedido?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _db.Pedidos
            .Include(x => x.Items)
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<PagedPedidos> ListAsync(
        PedidoStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var query = _db.Pedidos.Include(x => x.Items).AsNoTracking();
        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        var total = await query.CountAsync(cancellationToken);
        var skip = (int)Math.Min((long)(page - 1) * pageSize, int.MaxValue);
        var pedidos = await query.OrderByDescending(x => x.DataCreated)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedPedidos(pedidos, page, pageSize, total);
    }

    private async Task<Pedido> GetExistingPedidoAsync(Guid pedidoId, CancellationToken cancellationToken)
    {
        return await _db.Pedidos
            .Include(x => x.Items)
            .SingleAsync(x => x.Id == pedidoId, cancellationToken);
    }
 
}
