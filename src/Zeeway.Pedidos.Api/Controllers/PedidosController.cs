using Microsoft.AspNetCore.Mvc;
using Zeeway.Pedidos.Api.Mapping;
using Zeeway.Pedidos.Domain;
using Zeeway.Pedidos.Service.Interface;

namespace Zeeway.Pedidos.Api;

[ApiController]
[Route("pedidos")]
public sealed class PedidosController : ControllerBase
{
    private readonly IPedidoService _pedidos;

    public PedidosController(IPedidoService pedidos) => _pedidos = pedidos;

    [HttpPost]
    [ProducesResponseType(typeof(PedidoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePedidoRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.EmailCliente))
            return BadRequest();
        if (request.Items is null || request.Items.Count == 0)
            return BadRequest();
        if (request.Items.Any(x => x is null || string.IsNullOrWhiteSpace(x.Nome) || x.Quantidade <= 0 || x.PrecoUnitario < 0))
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
                request.EmailCliente,
                request.Items.Select(x => new CreatePedidoItemCommand(x.Nome, x.Quantidade, x.PrecoUnitario)).ToList(),
                idempotencyHeader.First()!.Trim()),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, PedidoApiMapper.ToResponse(pedido));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PedidoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PedidoResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var pedido = await _pedidos.GetByIdAsync(id, cancellationToken);
        return pedido is null ? NotFound() : Ok(PedidoApiMapper.ToResponse(pedido));
    }

    [HttpGet]
    public async Task<ActionResult<PagedPedidosResponse>> List([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        PedidoStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!PedidoApiMapper.TryParseStatus(status, out var parsedStatusValue))
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
            result.Items.Select(PedidoApiMapper.ToResponse).ToList(),
            result.Page,
            result.PageSize,
            result.Total));
    }
}
