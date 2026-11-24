namespace Zeeway.Orders.Infrastructure;

public interface IPaymentGatewayClient
{
    Task<CreatePaymentResponse> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken);
}
