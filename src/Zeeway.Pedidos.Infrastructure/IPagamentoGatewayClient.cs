namespace Zeeway.Pedidos.Infrastructure;

public interface IPagamentoGatewayClient
{
    Task<CreatePagamentoResponse> CreatePagamentoAsync(CreatePagamentoRequest request, CancellationToken cancellationToken);
}
