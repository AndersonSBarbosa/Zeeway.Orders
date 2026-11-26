using System.Text.Json;
using Zeeway.Pedidos.Domain;
using Zeeway.Pedidos.Infrastructure.Interface;
using Zeeway.Pedidos.Service.Interface;

namespace Zeeway.Pedidos.Service;

public sealed class PagamentoWebhookService : IPagamentoWebhookService
{
    private readonly IPagamentoWebhookRepository _repository;
    private readonly IWebhookSignatureValidator _signatureValidator;

    public PagamentoWebhookService(
        IPagamentoWebhookRepository repository,
        IWebhookSignatureValidator signatureValidator)
    {
        _repository = repository;
        _signatureValidator = signatureValidator;
    }

    public async Task<WebhookProcessingResult> ProcessAsync(
        string body,
        string? signature,
        CancellationToken cancellationToken)
    {
        var signatureStatus = _signatureValidator.Validate(body, signature);
        if (signatureStatus == WebhookSignatureStatus.NotConfigured)
            return WebhookProcessingResult.AssinaturaNaoConfigurada;
        if (signatureStatus != WebhookSignatureStatus.Valid)
            return WebhookProcessingResult.AssinaturaInvalida;

        var payload = TryParsePayload(body);
        if (payload is null)
            return WebhookProcessingResult.PayloadInvalido;

        if (await _repository.EventExistsAsync(payload.EventId, cancellationToken))
            return WebhookProcessingResult.Duplicado;

        var pedido = await _repository.GetPedidoAsync(payload.PedidoId, cancellationToken);
        if (pedido is null)
            return WebhookProcessingResult.EncomendaNaoEncontrada;

        try
        {
            pedido.AplicarPagamentoStatus(payload.Status);
        }
        catch (InvalidOperationException)
        {
            return WebhookProcessingResult.TransicaoDeEstadoInvalida;
        }

        var evento = new PagamentoEventRecord(
            payload.EventId,
            pedido.Id,
            payload.Type,
            DateTimeOffset.UtcNow);

        var saved = await _repository.SaveEventAsync(evento, cancellationToken);
        return saved ? WebhookProcessingResult.Aceite : WebhookProcessingResult.Duplicado;
    }

    private static WebhookPayload? TryParsePayload(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return null;

            var eventId = root.TryGetProperty("EventoId", out var eventIdElement)
                && eventIdElement.ValueKind == JsonValueKind.String
                    ? eventIdElement.GetString()
                    : null;
            var pedidoId = root.TryGetProperty("PedidoId", out var pedidoIdElement)
                && pedidoIdElement.ValueKind == JsonValueKind.String
                && Guid.TryParse(pedidoIdElement.GetString(), out var parsedPedidoId)
                    ? parsedPedidoId
                    : Guid.Empty;
            var type = root.TryGetProperty("Tipo", out var typeElement)
                && typeElement.ValueKind == JsonValueKind.String
                    ? typeElement.GetString()
                    : null;

            if (string.IsNullOrWhiteSpace(eventId) || pedidoId == Guid.Empty || type is null)
                return null;

            var status = type switch
            {
                "payment.approved" => PedidoStatus.Pago,
                "payment.failed" => PedidoStatus.Falha,
                _ => (PedidoStatus?)null
            };
            if (status is null)
                return null;

            return new WebhookPayload(eventId, pedidoId, type, status.Value);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record WebhookPayload(
        string EventId,
        Guid PedidoId,
        string Type,
        PedidoStatus Status);
}
