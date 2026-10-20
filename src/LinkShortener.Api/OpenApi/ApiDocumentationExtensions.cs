using System.Text;
using Microsoft.OpenApi.Models;

namespace LinkShortener.Api.OpenApi;

/// <summary>
/// Documentação OpenAPI e Swagger UI (RNF-02, D-05), publicadas sob /api/docs para não
/// ocupar códigos de link na raiz.
/// </summary>
public static class ApiDocumentationExtensions
{
    public const string DocumentName = "v1";
    public const string RoutePrefix = "api/docs";
    public const string DocumentUrl = $"/{RoutePrefix}/{DocumentName}/swagger.json";

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(DocumentName, new OpenApiInfo
            {
                Title = "Encurtador de links",
                Version = DocumentName,
                Description = "Cria links curtos, redireciona para o destino e informa o total de acessos.",
            });
            options.OperationFilter<LinkOperationFilter>();
            options.SchemaFilter<LinkExamplesSchemaFilter>();
        });

        return services;
    }

    /// <summary>
    /// Publica a documentação em todos os ambientes, não só em desenvolvimento.
    /// </summary>
    public static WebApplication UseApiDocumentation(this WebApplication app)
    {
        // O Swagger UI responderia /api/docs com um redirecionamento; servimos a página direto.
        app.Use((context, next) =>
        {
            if (context.Request.Path.Equals($"/{RoutePrefix}", StringComparison.OrdinalIgnoreCase)
                || context.Request.Path.Equals($"/{RoutePrefix}/", StringComparison.OrdinalIgnoreCase))
            {
                context.Request.Path = $"/{RoutePrefix}/index.html";
            }

            return next(context);
        });

        app.UseSwagger(options => options.RouteTemplate = $"{RoutePrefix}/{{documentName}}/swagger.json");
        app.UseSwaggerUI(options =>
        {
            options.RoutePrefix = RoutePrefix;
            options.DocumentTitle = "Encurtador de links - documentação";
            options.SwaggerEndpoint(DocumentUrl, $"Encurtador de links {DocumentName}");

            // Com a página servida em /api/docs (sem barra no fim), os arquivos relativos do
            // Swagger UI apontariam para /api; o <base> mantém todos sob /api/docs/.
            var defaultIndex = options.IndexStream;
            options.IndexStream = () => WithBaseHref(defaultIndex());
        });

        return app;
    }

    private static Stream WithBaseHref(Stream index)
    {
        using var reader = new StreamReader(index);
        var html = reader.ReadToEnd();

        var headStart = html.IndexOf("<head>", StringComparison.OrdinalIgnoreCase);
        if (headStart >= 0)
        {
            html = html.Insert(headStart + "<head>".Length, $"<base href=\"/{RoutePrefix}/\">");
        }

        return new MemoryStream(Encoding.UTF8.GetBytes(html));
    }
}
