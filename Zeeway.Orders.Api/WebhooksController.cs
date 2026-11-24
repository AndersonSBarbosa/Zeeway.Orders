using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zeeway.Orders.Domain;
using Zeeway.Orders.Infrastructure;

namespace Zeeway.Orders.Api;

[ApiController]
[Route("webhooks/payments")]
public sealed class WebhooksController : ControllerBase
{
    private readonly OrdersDbContext _db;
    private readonly IConfiguration _configuration;

    public WebhooksController(OrdersDbContext db, IConfiguration configuration)
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

        var payload = System.Text.Json.JsonSerializer.Deserialize<PaymentWebhook>(
            body, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (payload is null || payload.EventId is null || payload.OrderId == Guid.Empty)
            return BadRequest();

        if (await _db.PaymentEvents.AnyAsync(x => x.EventId == payload.EventId, cancellationToken))
            return Ok();

        var order = await _db.Orders.SingleOrDefaultAsync(x => x.Id == payload.OrderId, cancellationToken);
        if (order is null) return NotFound();

        try
        {
            var target = payload.Type switch
            {
                "payment.approved" => OrderStatus.Paid,
                "payment.failed" => OrderStatus.Failed,
                _ => (OrderStatus?)null
            };
            if (target is null) return BadRequest();

            if (order.ApplyPaymentStatus(target.Value))
            {
                await _db.PaymentEvents.AddAsync(
                    new PaymentEventRecord(payload.EventId, order.Id, payload.Type), cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                await _db.PaymentEvents.AddAsync(
                    new PaymentEventRecord(payload.EventId, order.Id, payload.Type), cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
            }
            return Ok();
        }
        catch (InvalidOperationException)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Invalid payment state transition",
                Detail = "The order is already in a final state."
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

    public sealed record PaymentWebhook(
        [property: System.Text.Json.Serialization.JsonPropertyName("event_id")] string? EventId,
        [property: System.Text.Json.Serialization.JsonPropertyName("order_id")] Guid OrderId,
        [property: System.Text.Json.Serialization.JsonPropertyName("type")] string Type);
}
