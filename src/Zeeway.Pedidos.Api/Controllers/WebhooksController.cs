using System.Text;
using Microsoft.AspNetCore.Mvc;
using Zeeway.Pedidos.Service.Interface;

namespace Zeeway.Pedidos.Api;

[ApiController]
[Route("webhooks/payments")]
public sealed class WebhooksController : ControllerBase
{
    private readonly IPagamentoWebhookService _webhookService;

    public WebhooksController(IPagamentoWebhookService webhookService) =>
        _webhookService = webhookService;

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["X-Signature-SHA256"].FirstOrDefault() ?? Request.Headers["X-Payment-Signature"].FirstOrDefault();

        var result = await _webhookService.ProcessAsync(body, signature, cancellationToken);
        return result switch
        {
            WebhookProcessingResult.Aceite or WebhookProcessingResult.Duplicado => Ok(),
            WebhookProcessingResult.AssinaturaInvalida => Unauthorized(),
            WebhookProcessingResult.PayloadInvalido => BadRequest(),
            WebhookProcessingResult.EncomendaNaoEncontrada => NotFound(),
            WebhookProcessingResult.TransicaoDeEstadoInvalida => Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Transação de pagamento inválida",
                Detail = "O pedido está no ultimo Status."
            }),
            WebhookProcessingResult.AssinaturaNaoConfigurada =>
                Problem(statusCode: StatusCodes.Status500InternalServerError, title: "A Chave do webhook não está configurado."),
            _ => throw new ArgumentOutOfRangeException(nameof(result), result, "Resultado de webhook desconhecido.")
        };
    }
}
