using System.Diagnostics;

namespace Zeeway.Pedidos.Api;

public sealed class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var values)
            && !string.IsNullOrWhiteSpace(values.First())
                ? values.First()!
                : Activity.Current?.Id ?? Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        var logger = context.RequestServices.GetRequiredService<ILogger<CorrelationIdMiddleware>>();
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        });
        await _next(context);
    }
}
