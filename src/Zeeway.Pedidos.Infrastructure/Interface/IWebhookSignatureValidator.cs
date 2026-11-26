namespace Zeeway.Pedidos.Infrastructure.Interface;

public interface IWebhookSignatureValidator
{
    WebhookSignatureStatus Validate(string body, string? signature);
}

public enum WebhookSignatureStatus
{
    Valid,
    Invalid,
    NotConfigured
}
