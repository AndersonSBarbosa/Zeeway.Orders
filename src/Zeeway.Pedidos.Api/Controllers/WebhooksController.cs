using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zeeway.Pedidos.Domain;
using Zeeway.Pedidos.Infrastructure;

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
            return Problem(statusCode: 500, title: "Webhook secret não está configurao.");

        var signature = Request.Headers["X-Signature-SHA256"].FirstOrDefault()
            ?? Request.Headers["X-Payment-Signature"].FirstOrDefault();
        if (!ValidSignature(body, signature, secret))
            return Unauthorized();

        string? eventId;
        Guid pedidoId;
        string? type;
        using (var payload = System.Text.Json.JsonDocument.Parse(body))
        {
            var root = payload.RootElement;
            eventId = root.TryGetProperty("event_id", out var eventElement)
                ? eventElement.GetString()
                : null;
            pedidoId = root.TryGetProperty("order_id", out var pedidoElement)
                && Guid.TryParse(pedidoElement.GetString(), out var parsedPedidoId)
                    ? parsedPedidoId
                    : Guid.Empty;
            type = root.TryGetProperty("type", out var typeElement)
                ? typeElement.GetString()
                : null;
        }
        if (eventId is null || pedidoId == Guid.Empty)
            return BadRequest();

        if (await _db.EventosPagamento.AnyAsync(x => x.EventId == eventId, cancellationToken))
            return Ok();

        var pedido = await _db.Pedidos.SingleOrDefaultAsync(x => x.Id == pedidoId, cancellationToken);
        if (pedido is null) return NotFound();

        try
        {
            var target = type switch
            {
                "payment.approved" => PedidoStatus.Paid,
                "payment.failed" => PedidoStatus.Failed,
                _ => (PedidoStatus?)null
            };
            if (target is null) return BadRequest();

            if (pedido.ApplyPagamentoStatus(target.Value))
            {
                await _db.EventosPagamento.AddAsync(
                    new PagamentoEventRecord(eventId, pedido.Id, type!), cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                await _db.EventosPagamento.AddAsync(
                    new PagamentoEventRecord(eventId, pedido.Id, type!), cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
            }
            return Ok();
        }
        catch (InvalidOperationException)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "pagamento inválida",
                Detail = "O Pedido já se encontra num estado final."
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
