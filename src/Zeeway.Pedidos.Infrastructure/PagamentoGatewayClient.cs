using System.Net.Http.Json;
using Zeeway.Pedidos.Infrastructure.Interface;
using Zeeway.Pedidos.Infrastructure.Model;

namespace Zeeway.Pedidos.Infrastructure;

public sealed class PagamentoGatewayClient : IPagamentoGatewayClient
{
    private readonly HttpClient _httpClient;

    public PagamentoGatewayClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<CreatePagamentoResponse> CreatePagamentoAsync(CreatePagamentoRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync("/payments", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CreatePagamentoResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Gateway de pagamento retornou uma resposta vazia.");
    }
}
