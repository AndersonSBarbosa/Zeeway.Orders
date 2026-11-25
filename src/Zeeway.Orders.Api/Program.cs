using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zeeway.Orders.Api;
using Zeeway.Orders.Infrastructure;
using Zeeway.Orders.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<IOrderService, OrderService>();

var connectionString = builder.Configuration.GetConnectionString("Orders")
    ?? "Host=localhost;Port=5432;Database=zeeway_orders;Username=zeeway;Password=zeeway";

builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgresql");

var gatewayBaseUrl = builder.Configuration["PaymentGateway:BaseUrl"] ?? "http://localhost:8081";
var gatewayHttp = builder.Services.AddHttpClient<IPaymentGatewayClient, PaymentGatewayClient>(client =>
{
    client.BaseAddress = new Uri(gatewayBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});
gatewayHttp.AddStandardResilienceHandler();

if (builder.Configuration.GetValue<bool>("PaymentGateway:UseFakeGateway"))
{
    builder.Services.AddTransient<PaymentGatewayMockHandler>();
    gatewayHttp.ConfigurePrimaryHttpMessageHandler(() => new PaymentGatewayMockHandler());
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
            Title = status == StatusCodes.Status500InternalServerError ? "Unexpected server error" : "Request could not be completed",
            Detail = status == StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred."
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
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    if (db.Database.IsRelational())
        await db.Database.MigrateAsync();
}

app.Run();

public partial class Program;
