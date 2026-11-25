using Zeeway.Pedidos.Infrastructure.Model;

namespace Zeeway.Pedidos.Infrastructure.Interface;

public interface IPagamentoGatewayClient
{
    Task<CreatePagamentoResponse> CreatePagamentoAsync(CreatePagamentoRequest request, CancellationToken cancellationToken);
}
