using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Zeeway.Pedidos.Infrastructure.Model;

public sealed class PagamentoGatewayMockHandler : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var payload = await request.Content!.ReadFromJsonAsync<CreatePagamentoRequest>(cancellationToken: cancellationToken);
        if (payload is null) return new HttpResponseMessage(HttpStatusCode.BadRequest);

        var pagamentoId = $"pay_{payload.PedidoId:N}";
        var body = JsonSerializer.SerializeToUtf8Bytes(new CreatePagamentoResponse(pagamentoId, "Pending"));
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(body)
            {
                Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json") }
            }
        };
    }
}
