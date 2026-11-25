using Microsoft.AspNetCore.Mvc;
using Zeeway.Orders.Domain;
using Zeeway.Orders.Service;

namespace Zeeway.Orders.Api;

[ApiController]
[Route("orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders) => _orders = orders;

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

        var order = await _orders.CreateAsync(
            new CreateOrderCommand(
                request.CustomerEmail,
                request.Items.Select(x => new CreateOrderItemCommand(x.Name, x.Quantity, x.UnitPrice)).ToList(),
                idempotencyHeader.First()!.Trim()),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, Map(order));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(id, cancellationToken);
        return order is null ? NotFound() : Ok(Map(order));
    }

    [HttpGet]
    public async Task<ActionResult<PagedOrdersResponse>> List(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        OrderStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<OrderStatus>(status, true, out var parsedStatusValue))
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid order status",
                    Detail = $"Status must be one of: {string.Join(", ", Enum.GetNames<OrderStatus>())}"
                });
            parsedStatus = parsedStatusValue;
        }

        var result = await _orders.ListAsync(parsedStatus, page, pageSize, cancellationToken);
        return Ok(new PagedOrdersResponse(
            result.Items.Select(Map).ToList(), result.Page, result.PageSize, result.Total));
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
