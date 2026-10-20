using LinkShortener.Api.Application;
using LinkShortener.Api.Data;
using LinkShortener.Api.Endpoints;
using LinkShortener.Api.OpenApi;

namespace LinkShortener.Api;

/// <summary>
/// Monta a aplicação. Fica fora do Program.cs para que os testes de integração
/// consigam subir a mesma aplicação com configuração própria.
/// </summary>
public static class ApiApplication
{
    public static WebApplication Build(WebApplicationOptions options, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(options);
        configure?.Invoke(builder);

        builder.Services.AddProblemDetails(problem =>
            problem.CustomizeProblemDetails = context =>
            {
                // Falhas de leitura da requisição (JSON malformado, tipo incorreto) chegam sem detalhe.
                if (context.ProblemDetails.Status == StatusCodes.Status400BadRequest && context.ProblemDetails.Detail is null)
                {
                    context.ProblemDetails.Title = "Requisição inválida";
                    context.ProblemDetails.Detail = "O corpo da requisição está malformado ou contém campos com tipo incorreto.";
                }
            });
        builder.Services.AddApiDocumentation();
        builder.Services.AddApplication();
        builder.Services.AddDatabase(builder.Configuration);

        var app = builder.Build();

        app.Services.ApplyMigrations();

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        app.UseApiDocumentation();

        app.MapLinkEndpoints();

        return app;
    }
}
