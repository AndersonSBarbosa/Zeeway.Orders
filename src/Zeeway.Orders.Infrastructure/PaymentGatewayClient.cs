using System.Net.Http.Json;

namespace Zeeway.Orders.Infrastructure;

public sealed class PaymentGatewayClient : IPaymentGatewayClient
{
    private readonly HttpClient _httpClient;

    public PaymentGatewayClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<CreatePaymentResponse> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync("/payments", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CreatePaymentResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Payment gateway returned an empty response.");
    }
}
