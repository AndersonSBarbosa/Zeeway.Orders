using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zeeway.Pedidos.Api;
using Zeeway.Pedidos.Infrastructure;
using Zeeway.Pedidos.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<IPedidoService, PedidoService>();

var connectionString = builder.Configuration.GetConnectionString("Orders");

builder.Services.AddDbContext<PedidosDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgresql");

var gatewayBaseUrl = builder.Configuration["PaymentGateway:BaseUrl"] ?? "http://localhost:8081";
var gatewayHttp = builder.Services.AddHttpClient<IPagamentoGatewayClient, PagamentoGatewayClient>(client =>
{
    client.BaseAddress = new Uri(gatewayBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});
gatewayHttp.AddStandardResilienceHandler();

if (builder.Configuration.GetValue<bool>("PaymentGateway:UseFakeGateway"))
{
    builder.Services.AddTransient<PagamentoGatewayMockHandler>();
    gatewayHttp.ConfigurePrimaryHttpMessageHandler(() => new PagamentoGatewayMockHandler());
}

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var status = feature?.Error switch
        {
            DbUpdateConcurrencyException => StatusCodes.Status409Conflict,
            HttpRequestException or TimeoutException => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status500InternalServerError
        };
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = status == StatusCodes.Status500InternalServerError ? "Erro inesperado do servidor" : "A solicitacao nao pode ser concluida",
            Detail = status == StatusCodes.Status500InternalServerError
                ? "Ocorreu um erro inesperado."
                : feature?.Error.Message
        });
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PedidosDbContext>();
    if (db.Database.IsRelational())
        await db.Database.MigrateAsync();
}

app.Run();

public partial class Program;
