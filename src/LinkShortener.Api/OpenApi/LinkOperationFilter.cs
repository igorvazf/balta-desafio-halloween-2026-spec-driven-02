using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LinkShortener.Api.OpenApi;

/// <summary>
/// Completa as operações com o que o ApiExplorer não descreve: descrição das respostas,
/// exemplo do parâmetro de código e o header Location dos redirecionamentos.
/// </summary>
public sealed class LinkOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        foreach (var metadata in context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<ResponseDescriptionMetadata>())
        {
            if (operation.Responses.TryGetValue(metadata.StatusCode.ToString(), out var response))
            {
                response.Description = metadata.Description;
            }
        }

        foreach (var parameter in operation.Parameters.Where(p => p.Name == "code"))
        {
            parameter.Description = "Código do link: de 4 a 20 caracteres, letras maiúsculas de A a Z e dígitos de 1 a 9.";
            parameter.Example = new OpenApiString("PROMO26");
        }

        foreach (var status in new[] { "201", "302" })
        {
            if (operation.Responses.TryGetValue(status, out var response))
            {
                response.Headers["Location"] = new OpenApiHeader
                {
                    Description = status == "302" ? "URL de destino do link." : "Link curto criado.",
                    Schema = new OpenApiSchema { Type = "string", Format = "uri" },
                };
            }
        }
    }
}
