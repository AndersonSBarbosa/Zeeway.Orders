using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zeeway.Orders.Domain;
using Zeeway.Orders.Infrastructure;

namespace Zeeway.Orders.Service;

public sealed class OrderService : IOrderService
{
    private readonly OrdersDbContext _db;
    private readonly IPaymentGatewayClient _gateway;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        OrdersDbContext db,
        IPaymentGatewayClient gateway,
        ILogger<OrderService> logger)
    {
        _db = db;
        _gateway = gateway;
        _logger = logger;
    }

    public async Task<Order> CreateAsync(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var existing = await _db.IdempotencyRecords.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Key == command.IdempotencyKey, cancellationToken);
        if (existing is not null)
            return await GetExistingOrderAsync(existing.OrderId, cancellationToken);

        var order = new Order(
            command.CustomerEmail,
            command.Items.Select(x => new OrderItem(x.Name, x.Quantity, x.UnitPrice)));
        await _db.Orders.AddAsync(order, cancellationToken);
        await _db.IdempotencyRecords.AddAsync(
            new IdempotencyRecord(command.IdempotencyKey, order.Id),
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
            return await GetExistingOrderAsync(winner.OrderId, cancellationToken);
        }

        try
        {
            var payment = await _gateway.CreatePaymentAsync(
                new CreatePaymentRequest(order.Id, order.TotalAmount, order.CustomerEmail),
                cancellationToken);
            order.SetPaymentId(payment.PaymentId);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Payment creation failed for order {OrderId}", order.Id);
        }

        return order;
    }

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Orders.Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<PagedOrders> ListAsync(
        OrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var query = _db.Orders.Include(x => x.Items).AsNoTracking();
        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        var total = await query.CountAsync(cancellationToken);
        var orders = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedOrders(orders, page, pageSize, total);
    }

    private async Task<Order> GetExistingOrderAsync(Guid orderId, CancellationToken cancellationToken) =>
        await _db.Orders.Include(x => x.Items)
            .SingleAsync(x => x.Id == orderId, cancellationToken);
}
