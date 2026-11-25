using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Zeeway.Pedidos.Api;

public sealed class WebhookOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;
        if (!string.Equals(apiDescription.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(
                apiDescription.RelativePath?.Trim('/'),
                "webhooks/payments",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        operation.Parameters ??= new List<OpenApiParameter>();
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Signature-SHA256",
            In = ParameterLocation.Header,
            Required = true,
            Description = "Hex HMAC-SHA256 da string exata do corpo, usando PaymentGateway:WebhookSecret.",
            Schema = new OpenApiSchema { Type = "string" }
        });

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Description = "Use payment.approved ou payment.failed. A assinatura deve corresponder exatamente a este JSON.",
            Content =
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Required = new HashSet<string> { "event_id", "order_id", "type" },
                        Properties = new Dictionary<string, OpenApiSchema>
                        {
                            ["event_id"] = new() { Type = "string" },
                            ["order_id"] = new() { Type = "string", Format = "uuid" },
                            ["type"] = new() { Type = "string" }
                        }
                    }
                }
            }
        };
    }
}
