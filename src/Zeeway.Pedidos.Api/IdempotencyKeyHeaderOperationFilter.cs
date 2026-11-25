using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Zeeway.Pedidos.Api;

public sealed class IdempotencyKeyHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;
        if (!string.Equals(apiDescription.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(
                apiDescription.RelativePath?.Trim('/'),
                "pedidos",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        operation.Parameters ??= new List<OpenApiParameter>();
        if (operation.Parameters.Any(parameter =>
            parameter.In == ParameterLocation.Header
            && string.Equals(parameter.Name, "Idempotency-Key", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "Idempotency-Key",
            In = ParameterLocation.Header,
            Required = true,
            Description = "Chave obrigatoria; reutilize a mesma chave ao repetir a mesma operacao.",
            Schema = new OpenApiSchema
            {
                Type = "string",
                MinLength = 1
            }
        });
    }
}
