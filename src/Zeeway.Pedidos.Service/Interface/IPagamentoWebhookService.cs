namespace Zeeway.Pedidos.Service.Interface;

public interface IPagamentoWebhookService
{
    Task<WebhookProcessingResult> ProcessAsync(
        string body,
        string? signature,
        CancellationToken cancellationToken);
}

public enum WebhookProcessingResult
{
    Aceite,
    Duplicado,
    AssinaturaInvalida,
    PayloadInvalido,
    EncomendaNaoEncontrada,
    TransicaoDeEstadoInvalida,
    AssinaturaNaoConfigurada
}
