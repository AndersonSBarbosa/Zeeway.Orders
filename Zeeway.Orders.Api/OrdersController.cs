using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zeeway.Orders.Domain;
using Zeeway.Orders.Infrastructure;

namespace Zeeway.Orders.Api;

[ApiController]
[Route("orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly OrdersDbContext _db;
    private readonly IPaymentGatewayClient _gateway;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(OrdersDbContext db, IPaymentGatewayClient gateway, ILogger<OrdersController> logger)
    {
        _db = db;
        _gateway = gateway;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerEmail))
            return BadRequest();
        if (request.Items is null || request.Items.Count == 0)
            return BadRequest();
        if (request.Items.Any(x => string.IsNullOrWhiteSpace(x.Name)
            || x.Quantity <= 0
            || x.UnitPrice < 0))
            return BadRequest();

        if (!Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyHeader)
            || string.IsNullOrWhiteSpace(idempotencyHeader.First()))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Missing Idempotency-Key",
                detail: "The Idempotency-Key header is required.");
        }

        var key = idempotencyHeader.First()!.Trim();
        var existing = await _db.IdempotencyRecords.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Key == key, cancellationToken);
        if (existing is not null)
        {
            var existingOrder = await _db.Orders.Include(x => x.Items)
                .SingleAsync(x => x.Id == existing.OrderId, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, Map(existingOrder));
        }

        var items = request.Items.Select(x => new OrderItem(x.Name, x.Quantity, x.UnitPrice));
        var order = new Order(request.CustomerEmail, items);
        await _db.Orders.AddAsync(order, cancellationToken);
        await _db.IdempotencyRecords.AddAsync(new IdempotencyRecord(key, order.Id), cancellationToken);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            var winner = await _db.IdempotencyRecords.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Key == key, cancellationToken);
            if (winner is null) throw;
            var winnerOrder = await _db.Orders.Include(x => x.Items)
                .SingleAsync(x => x.Id == winner.OrderId, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, Map(winnerOrder));
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

        return StatusCode(StatusCodes.Status201Created, Map(order));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _db.Orders.Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return order is null ? NotFound() : Ok(Map(order));
    }

    [HttpGet]
    public async Task<ActionResult<PagedOrdersResponse>> List(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var query = _db.Orders.Include(x => x.Items).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<OrderStatus>(status, true, out var parsed))
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid order status",
                    Detail = $"Status must be one of: {string.Join(", ", Enum.GetNames<OrderStatus>())}"
                });
            query = query.Where(x => x.Status == parsed);
        }

        var total = await query.CountAsync(cancellationToken);
        var orders = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return Ok(new PagedOrdersResponse(
            orders.Select(Map).ToList(), page, pageSize, total));
    }

    private static OrderResponse Map(Order order) => new(
        order.Id,
        order.TotalAmount,
        order.CustomerEmail,
        order.Status.ToString(),
        order.PaymentId,
        order.CreatedAt,
        order.UpdatedAt,
        order.Items.Select(x => new OrderItemResponse(x.Id, x.Name, x.Quantity, x.UnitPrice)).ToList());
}
