using LinkShortener.Api.Endpoints;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LinkShortener.Api.OpenApi;

/// <summary>
/// Exemplos e descrições dos contratos de entrada e saída.
/// </summary>
public sealed class LinkExamplesSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(CreateLinkRequest))
        {
            schema.Required.Add("targetUrl");
            Describe(schema, "targetUrl", "URL de destino, http ou https, com até 2048 caracteres e host público.");
            Describe(schema, "code", "Código próprio opcional: de 4 a 20 caracteres, letras maiúsculas de A a Z e dígitos de 1 a 9. Sem ele, um código de 7 caracteres é gerado.");
            Describe(schema, "expiresAtUtc", "Último dia de validade, opcional, interpretado em UTC. O link expira às 00:00 UTC do dia seguinte. A data atual é aceita.");
            schema.Example = new OpenApiObject
            {
                ["targetUrl"] = new OpenApiString("https://www.exemplo.com/artigos/2026/10/encurtador-de-links"),
                ["code"] = new OpenApiString("PROMO26"),
                ["expiresAtUtc"] = new OpenApiString("2026-10-31"),
            };
        }
        else if (context.Type == typeof(CreateLinkResponse))
        {
            schema.Example = new OpenApiObject
            {
                ["shortUrl"] = new OpenApiString("https://encurta.exemplo.com/PROMO26"),
                ["code"] = new OpenApiString("PROMO26"),
            };
        }
        else if (context.Type == typeof(LinkStatsResponse))
        {
            schema.Example = new OpenApiObject
            {
                ["code"] = new OpenApiString("PROMO26"),
                ["accessCount"] = new OpenApiLong(42),
            };
        }
    }

    private static void Describe(OpenApiSchema schema, string property, string description)
    {
        if (schema.Properties.TryGetValue(property, out var propertySchema))
        {
            propertySchema.Description = description;
        }
    }
}
