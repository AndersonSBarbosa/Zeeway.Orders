namespace Zeeway.Orders.Infrastructure;

public sealed record CreatePaymentRequest(Guid OrderId, decimal Amount, string CustomerEmail);
public sealed record CreatePaymentResponse(string PaymentId, string Status);
