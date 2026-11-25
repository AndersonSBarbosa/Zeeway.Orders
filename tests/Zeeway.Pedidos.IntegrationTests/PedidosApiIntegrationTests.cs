using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zeeway.Pedidos.Infrastructure;

namespace Zeeway.Pedidos.IntegrationTests;

public sealed class PedidosApiIntegrationTests : IClassFixture<PedidosApiFactory>
{
    private readonly PedidosApiFactory _factory;

    public PedidosApiIntegrationTests(PedidosApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Post_is_idempotent_and_get_returns_pedido()
    {
        var client = _factory.CreateClient();
        var request = new
        {
            CustomerEmail = "integration@example.com",
            Items = new[] { new { Name = "Notebook", Quantity = 1, UnitPrice = 99.90m } }
        };

        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/orders");
        firstRequest.Headers.Add("Idempotency-Key", "integration-key");
        firstRequest.Content = JsonContent.Create(request);

        var first = await client.SendAsync(firstRequest);
        Assert.True(first.StatusCode == HttpStatusCode.Created,
            await first.Content.ReadAsStringAsync());
        var firstJson = await first.Content.ReadFromJsonAsync<JsonElement>();
        var id = firstJson.GetProperty("id").GetGuid();
        Assert.True(firstJson.TryGetProperty("paymentId", out _));
        Assert.False(firstJson.TryGetProperty("pagamentoId", out _));
        Assert.Equal($"pay_{id:N}", firstJson.GetProperty("paymentId").GetString());

        using var secondRequest = new HttpRequestMessage(HttpMethod.Post, "/orders");
        secondRequest.Headers.Add("Idempotency-Key", "integration-key");
        secondRequest.Content = JsonContent.Create(request);
        var second = await client.SendAsync(secondRequest);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var secondJson = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(id, secondJson.GetProperty("id").GetGuid());

        var get = await client.GetFromJsonAsync<JsonElement>($"/orders/{id}");
        Assert.Equal(id, get.GetProperty("id").GetGuid());
        Assert.Equal("Pending", get.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Duplicate_webhook_is_accepted_only_once()
    {
        var client = _factory.CreateClient();
        var request = new
        {
            CustomerEmail = "webhook@example.com",
            Items = new[] { new { Name = "Keyboard", Quantity = 1, UnitPrice = 40m } }
        };
        using var create = new HttpRequestMessage(HttpMethod.Post, "/orders");
        create.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        create.Content = JsonContent.Create(request);
        var createResponse = await client.SendAsync(create);
        var pedido = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = pedido.GetProperty("id").GetGuid();

        var body = JsonSerializer.Serialize(new
        {
            event_id = "evt_" + Guid.NewGuid().ToString("N"),
            order_id = id,
            type = "payment.approved"
        });
        var hash = Convert.ToHexString(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes("development-webhook-secret"),
            Encoding.UTF8.GetBytes(body))).ToLowerInvariant();

        using var webhook1 = new StringContent(body, Encoding.UTF8, "application/json");
        webhook1.Headers.Add("X-Signature-SHA256", hash);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/webhooks/payments", webhook1)).StatusCode);

        using var webhook2 = new StringContent(body, Encoding.UTF8, "application/json");
        webhook2.Headers.Add("X-Signature-SHA256", hash);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/webhooks/payments", webhook2)).StatusCode);

        var updated = await client.GetFromJsonAsync<JsonElement>($"/orders/{id}");
        Assert.Equal("Paid", updated.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Malformed_signed_webhook_returns_bad_request()
    {
        var client = _factory.CreateClient();
        const string body = "{";
        var hash = Convert.ToHexString(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes("development-webhook-secret"),
            Encoding.UTF8.GetBytes(body))).ToLowerInvariant();

        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        content.Headers.Add("X-Signature-SHA256", hash);
        var response = await client.PostAsync("/webhooks/payments", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Numeric_undefined_status_filter_returns_bad_request()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/pedidos?status=999");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_with_null_item_returns_bad_request()
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/pedidos");
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        request.Content = new StringContent(
            "{\"customerEmail\":\"null-item@example.com\",\"items\":[null]}",
            Encoding.UTF8,
            "application/json");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public sealed class PedidosApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"PedidosApi-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<PedidosDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<PedidosDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}
