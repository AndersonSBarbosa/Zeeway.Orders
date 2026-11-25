using Microsoft.AspNetCore.Mvc;
using Zeeway.Pedidos.Domain;
using Zeeway.Pedidos.Service;

namespace Zeeway.Pedidos.Api;

[ApiController]
[Route("pedidos")]
[Route("orders")]
public sealed class PedidosController : ControllerBase
{
    private readonly IPedidoService _pedidos;

    public PedidosController(IPedidoService pedidos) => _pedidos = pedidos;

    [HttpPost]
    [ProducesResponseType(typeof(PedidoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePedidoRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerEmail))
            return BadRequest();
        if (request.Items is null || request.Items.Count == 0)
            return BadRequest();
        if (request.Items.Any(x => x is null
            || string.IsNullOrWhiteSpace(x.Name)
            || x.Quantity <= 0
            || x.UnitPrice < 0))
            return BadRequest();

        if (!Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyHeader)
            || string.IsNullOrWhiteSpace(idempotencyHeader.First()))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Chave Ausente",
                detail: "Chave obrigatoria.");
        }

        var pedido = await _pedidos.CreateAsync(
            new CreatePedidoCommand(
                request.CustomerEmail,
                request.Items.Select(x => new CreatePedidoItemCommand(x.Name, x.Quantity, x.UnitPrice)).ToList(),
                idempotencyHeader.First()!.Trim()),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, Map(pedido));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PedidoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PedidoResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var pedido = await _pedidos.GetByIdAsync(id, cancellationToken);
        return pedido is null ? NotFound() : Ok(Map(pedido));
    }

    [HttpGet]
    public async Task<ActionResult<PagedPedidosResponse>> List(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        PedidoStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!TryParseStatus(status, out var parsedStatusValue))
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Estado do pedido invalido",
                    Detail = "Use Pending, Paid, Failed, Pendente, Pago ou Falha."
                });
            parsedStatus = parsedStatusValue;
        }

        var result = await _pedidos.ListAsync(parsedStatus, page, pageSize, cancellationToken);
        return Ok(new PagedPedidosResponse(
            result.Items.Select(Map).ToList(), result.Page, result.PageSize, result.Total));
    }

    private static PedidoResponse Map(Pedido pedido) => new(
        pedido.Id,
        pedido.ValorTotal,
        pedido.EmailCliente,
        MapStatus(pedido.Status),
        pedido.PagamentoId,
        pedido.DataCreated,
        pedido.DataUpdated,
        pedido.Items.Select(x => new PedidoItemResponse(x.Id, x.Nome, x.Quantidade, x.PrecoUnitario)).ToList());

    private static bool TryParseStatus(string status, out PedidoStatus parsed) =>
        status.Trim().ToLowerInvariant() switch
        {
            "pending" or "pendente" => SetStatus(PedidoStatus.Pendente, out parsed),
            "paid" or "pago" => SetStatus(PedidoStatus.Pago, out parsed),
            "failed" or "falha" => SetStatus(PedidoStatus.Falha, out parsed),
            _ => SetStatus(default, out parsed, false)
        };

    private static bool SetStatus(PedidoStatus status, out PedidoStatus parsed, bool success = true)
    {
        parsed = status;
        return success;
    }

    private static string MapStatus(PedidoStatus status) => status switch
    {
        PedidoStatus.Pendente => "Pending",
        PedidoStatus.Pago => "Paid",
        PedidoStatus.Falha => "Failed",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown pedido status.")
    };
}
