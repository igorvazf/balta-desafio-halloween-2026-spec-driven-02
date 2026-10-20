using LinkShortener.Api.Application;
using LinkShortener.Api.OpenApi;

namespace LinkShortener.Api.Endpoints;

/// <summary>
/// Endpoints do recurso link. Só leem a requisição, chamam o <see cref="LinkService"/>
/// e traduzem o resultado; nenhuma regra de negócio é verificada aqui.
/// </summary>
public static class LinkEndpoints
{
    public static IEndpointRouteBuilder MapLinkEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/links", CreateAsync)
            .WithName("CreateLink")
            .WithTags("Links")
            .WithSummary("Cria um link curto")
            .WithDescription("Recebe a URL de destino e, opcionalmente, um código próprio e o último dia de validade em UTC. Sem código próprio, gera um código de 7 caracteres.")
            .Produces<CreateLinkResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .WithResponseDescription(StatusCodes.Status201Created, "Link criado.")
            .WithResponseDescription(StatusCodes.Status400BadRequest, "Requisição inválida: URL de destino recusada, código fora do formato, data de expiração no passado ou corpo malformado.")
            .WithResponseDescription(StatusCodes.Status409Conflict, "O código próprio informado já está em uso.")
            .WithResponseDescription(StatusCodes.Status503ServiceUnavailable, "Os 5 códigos gerados já estavam em uso; nenhum link foi criado.");

        app.MapGet("/api/links/{code}/stats", GetStatsAsync)
            .WithName("GetLinkStats")
            .WithTags("Links")
            .WithSummary("Consulta o total de acessos de um link")
            .WithDescription("Devolve o total de acessos persistido, para links válidos ou expirados. Não exige autenticação nem header.")
            .Produces<LinkStatsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithResponseDescription(StatusCodes.Status200OK, "Total de acessos do link.")
            .WithResponseDescription(StatusCodes.Status404NotFound, "Não existe link com esse código.");

        app.MapGet("/{code}", RedirectAsync)
            .WithName("RedirectToTarget")
            .WithTags("Redirecionamento")
            .WithSummary("Redireciona para a URL de destino")
            .WithDescription("Responde 302 com o destino no header Location e conta o acesso. Link expirado não redireciona e não conta acesso.")
            .Produces(StatusCodes.Status302Found)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status410Gone)
            .WithResponseDescription(StatusCodes.Status302Found, "Redirecionamento para a URL de destino.")
            .WithResponseDescription(StatusCodes.Status404NotFound, "Não existe link com esse código.")
            .WithResponseDescription(StatusCodes.Status410Gone, "O link expirou.");

        return app;
    }

    private static async Task<IResult> GetStatsAsync(string code, LinkService service, CancellationToken cancellationToken)
    {
        var result = await service.GetStatsAsync(code, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(new LinkStatsResponse(result.Value.Code, result.Value.AccessCount))
            : LinkErrorResults.ToResult(result.Error);
    }

    private static async Task<IResult> RedirectAsync(string code, LinkService service, CancellationToken cancellationToken)
    {
        var result = await service.ResolveAsync(code, cancellationToken);

        return result.IsSuccess
            ? Results.Redirect(result.Value, permanent: false)
            : LinkErrorResults.ToResult(result.Error);
    }

    private static async Task<IResult> CreateAsync(
        CreateLinkRequest? request,
        LinkService service,
        HttpRequest httpRequest,
        CancellationToken cancellationToken)
    {
        if (request?.TargetUrl is null)
        {
            return LinkErrorResults.ValidationProblem("targetUrl", "O campo targetUrl é obrigatório.");
        }

        var result = await service.CreateAsync(new CreateLinkCommand(request.TargetUrl, request.Code, request.ExpiresAtUtc), cancellationToken);
        if (!result.IsSuccess)
        {
            return LinkErrorResults.ToResult(result.Error);
        }

        var code = result.Value.Code;
        var shortUrl = $"{httpRequest.Scheme}://{httpRequest.Host}{httpRequest.PathBase}/{code}";

        return Results.Created(shortUrl, new CreateLinkResponse(shortUrl, code));
    }
}
