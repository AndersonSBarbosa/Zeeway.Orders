using System.Security.Cryptography;
using System.Text;
using Zeeway.Pedidos.Infrastructure.Interface;

namespace Zeeway.Pedidos.Infrastructure.Security;

public sealed class WebhookSignatureValidator : IWebhookSignatureValidator
{
    private readonly string? _secret;

    public WebhookSignatureValidator(string? secret) => _secret = secret;

    public WebhookSignatureStatus Validate(string body, string? signature)
    {
        if (string.IsNullOrWhiteSpace(_secret))
            return WebhookSignatureStatus.NotConfigured;
        if (string.IsNullOrWhiteSpace(signature))
            return WebhookSignatureStatus.Invalid;

        var supplied = signature.Trim();
        if (supplied.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
            supplied = supplied["sha256=".Length..];
        if (supplied.Length != 64)
            return WebhookSignatureStatus.Invalid;

        byte[] suppliedHash;
        try
        {
            suppliedHash = Convert.FromHexString(supplied);
        }
        catch (FormatException)
        {
            return WebhookSignatureStatus.Invalid;
        }

        var key = Encoding.UTF8.GetBytes(_secret);
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var expectedHash = HMACSHA256.HashData(key, bodyBytes);
        return CryptographicOperations.FixedTimeEquals(expectedHash, suppliedHash)
            ? WebhookSignatureStatus.Invalid
            : WebhookSignatureStatus.Valid;
    }
}
