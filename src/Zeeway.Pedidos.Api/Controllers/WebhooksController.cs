using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zeeway.Pedidos.Domain;
using Zeeway.Pedidos.Infrastructure.db;

namespace Zeeway.Pedidos.Api;

[ApiController]
[Route("webhooks/payments")]
public sealed class WebhooksController : ControllerBase
{
    private readonly PedidosDbContext _db;
    private readonly IConfiguration _configuration;

    public WebhooksController(PedidosDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync(cancellationToken);
        var secret = _configuration["PaymentGateway:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(secret))
            return Problem(statusCode: 500, title: "Webhook secret is not configured.");

        var signature = Request.Headers["X-Signature-SHA256"].FirstOrDefault()
            ?? Request.Headers["X-Payment-Signature"].FirstOrDefault();
        if (!ValidSignature(body, signature, secret))
            return Unauthorized();

        string? eventId;
        Guid pedidoId;
        string? type;
        try
        {
            using var payload = System.Text.Json.JsonDocument.Parse(body);
            var root = payload.RootElement;
            if (root.ValueKind != System.Text.Json.JsonValueKind.Object)
                return BadRequest();

            eventId = root.TryGetProperty("event_id", out var eventElement)
                && eventElement.ValueKind == System.Text.Json.JsonValueKind.String
                    ? eventElement.GetString()
                    : null;
            pedidoId = root.TryGetProperty("order_id", out var pedidoElement)
                && pedidoElement.ValueKind == System.Text.Json.JsonValueKind.String
                && Guid.TryParse(pedidoElement.GetString(), out var parsedPedidoId)
                    ? parsedPedidoId
                    : Guid.Empty;
            type = root.TryGetProperty("type", out var typeElement)
                && typeElement.ValueKind == System.Text.Json.JsonValueKind.String
                    ? typeElement.GetString()
                    : null;
        }
        catch (System.Text.Json.JsonException)
        {
            return BadRequest();
        }

        if (string.IsNullOrWhiteSpace(eventId) || pedidoId == Guid.Empty)
            return BadRequest();

        if (await _db.EventosPagamento.AnyAsync(x => x.EventoId == eventId, cancellationToken))
            return Ok();

        var pedido = await _db.Pedidos.SingleOrDefaultAsync(x => x.Id == pedidoId, cancellationToken);
        if (pedido is null) return NotFound();

        try
        {
            var target = type switch
            {
                "payment.approved" => PedidoStatus.Pago,
                "payment.failed" => PedidoStatus.Falha,
                _ => (PedidoStatus?)null
            };
            if (target is null) return BadRequest();

            pedido.AplicarPagamentoStatus(target.Value);
            await _db.EventosPagamento.AddAsync(
                new PagamentoEventRecord(eventId, pedido.Id, type!, DateTimeOffset.UtcNow),
                cancellationToken);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                return Ok();
            }
            catch (DbUpdateException)
            {
                if (await _db.EventosPagamento.AsNoTracking()
                    .AnyAsync(x => x.EventoId == eventId, cancellationToken))
                    return Ok();
                throw;
            }
        }
        catch (InvalidOperationException)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Invalid payment transition",
                Detail = "The pedido is already in a final state."
            });
        }
    }

    private static bool ValidSignature(string body, string? signature, string secret)
    {
        if (string.IsNullOrWhiteSpace(signature)) return false;
        var supplied = signature.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase)
            ? signature["sha256=".Length..]
            : signature;
        var key = Encoding.UTF8.GetBytes(secret);
        var hash = Convert.ToHexString(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(hash),
            Encoding.ASCII.GetBytes(supplied.Trim().ToLowerInvariant()));
    }

    public sealed record PagamentoWebhook(
        [property: System.Text.Json.Serialization.JsonPropertyName("event_id")] string? EventId,
        [property: System.Text.Json.Serialization.JsonPropertyName("order_id")] Guid PedidoId,
        [property: System.Text.Json.Serialization.JsonPropertyName("type")] string Type);
}
