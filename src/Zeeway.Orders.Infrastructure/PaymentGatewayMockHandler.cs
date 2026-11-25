using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Zeeway.Orders.Infrastructure;

public sealed class PaymentGatewayMockHandler : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var payload = await request.Content!.ReadFromJsonAsync<CreatePaymentRequest>(cancellationToken: cancellationToken);
        if (payload is null) return new HttpResponseMessage(HttpStatusCode.BadRequest);

        var paymentId = $"pay_{payload.OrderId:N}";
        var body = JsonSerializer.SerializeToUtf8Bytes(new CreatePaymentResponse(paymentId, "Pending"));
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(body)
            {
                Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json") }
            }
        };
    }
}
