using LinkShortener.Api.Domain;

namespace LinkShortener.Api.Application;

/// <param name="ExpirationDate">Último dia, em UTC, em que o link é válido. Nulo quando o link não expira.</param>
public sealed record CreateLinkCommand(string TargetUrl, string? Code = null, DateOnly? ExpirationDate = null);

public sealed record LinkStats(string Code, long AccessCount);

/// <summary>
/// Dono das regras de negócio dos links (RN-01 a RN-06).
/// </summary>
public sealed class LinkService(
    ILinkRepository repository,
    ICodeGenerator codeGenerator,
    TargetUrlValidator targetUrlValidator,
    LinkResolutionCache resolutionCache,
    TimeProvider clock)
{
    public const int MaxGeneratedCodeAttempts = 5;

    public async Task<Result<Link>> CreateAsync(CreateLinkCommand command, CancellationToken cancellationToken)
    {
        if (command.Code is not null && LinkCode.ValidateCustom(command.Code) is { } codeError)
        {
            return new LinkError(LinkErrorType.Validation, codeError, "code");
        }

        var now = clock.GetUtcNow();
        if (command.ExpirationDate is { } expirationDate && LinkExpiration.IsInThePast(expirationDate, now))
        {
            return new LinkError(LinkErrorType.Validation, "A data de expiração não pode ser anterior à data atual em UTC.", "expiresAtUtc");
        }

        var targetUrl = await targetUrlValidator.ValidateAsync(command.TargetUrl, cancellationToken);
        if (!targetUrl.IsValid)
        {
            return new LinkError(LinkErrorType.Validation, targetUrl.Error!, "targetUrl");
        }

        var expiresAtUtc = command.ExpirationDate is { } date ? LinkExpiration.ExpiresAtUtc(date) : (DateTime?)null;
        Link NewLink(string code) => new(code, targetUrl.NormalizedUrl!, now.UtcDateTime, expiresAtUtc);

        return command.Code is null
            ? await CreateWithGeneratedCodeAsync(NewLink, cancellationToken)
            : await CreateWithCustomCodeAsync(NewLink(command.Code), cancellationToken);
    }

    /// <summary>
    /// Descobre o destino de um código para o redirecionamento (RF-04) e conta o acesso (RF-05).
    /// O destino de um código acessado recentemente vem do cache, sem consultar o banco (RNF-01).
    /// </summary>
    public async Task<Result<string>> ResolveAsync(string code, CancellationToken cancellationToken)
    {
        if (!resolutionCache.TryGet(code, out var link))
        {
            var stored = await repository.GetByCodeAsync(code, cancellationToken);
            if (stored is null)
            {
                // Código inexistente não vai para o cache: ele ainda pode ser criado.
                return NotFound(code);
            }

            link = ResolvedLink.From(stored);
            resolutionCache.Set(code, link);
        }

        // RN-03: link expirado não redireciona e não conta acesso.
        if (LinkExpiration.IsExpired(link.ExpiresAtUtc, clock.GetUtcNow()))
        {
            return new LinkError(LinkErrorType.Expired, $"O link com o código '{code}' expirou e não redireciona mais.");
        }

        // RN-04: o contador só anda para frente, um acesso por vez, no próprio banco.
        await repository.IncrementAccessCountAsync(link.Id, cancellationToken);

        return link.TargetUrl;
    }

    /// <summary>
    /// Total de acessos de um link, expirado ou não (RF-07). Sempre lido do banco, nunca do cache.
    /// </summary>
    public async Task<Result<LinkStats>> GetStatsAsync(string code, CancellationToken cancellationToken)
    {
        var link = await repository.GetByCodeAsync(code, cancellationToken);
        if (link is null)
        {
            return NotFound(code);
        }

        return new LinkStats(link.Code, link.AccessCount);
    }

    private static LinkError NotFound(string code) =>
        new(LinkErrorType.NotFound, $"O link com o código '{code}' não existe.");

    private async Task<Result<Link>> CreateWithCustomCodeAsync(Link link, CancellationToken cancellationToken)
    {
        // RN-01: a checagem dá a resposta comum; o índice único cobre a corrida entre duas criações (D-04).
        if (await repository.ExistsByCodeAsync(link.Code, cancellationToken) || !await repository.TryAddAsync(link, cancellationToken))
        {
            return new LinkError(LinkErrorType.Conflict, $"O código '{link.Code}' já está em uso. Escolha outro código.", "code");
        }

        return link;
    }

    private async Task<Result<Link>> CreateWithGeneratedCodeAsync(Func<string, Link> newLink, CancellationToken cancellationToken)
    {
        // RN-06: código gerado em uso gera outro, até o limite de tentativas.
        for (var attempt = 1; attempt <= MaxGeneratedCodeAttempts; attempt++)
        {
            var code = codeGenerator.Generate();
            if (await repository.ExistsByCodeAsync(code, cancellationToken))
            {
                continue;
            }

            var link = newLink(code);
            if (await repository.TryAddAsync(link, cancellationToken))
            {
                return link;
            }
        }

        return new LinkError(
            LinkErrorType.CodeGenerationFailed,
            $"Não foi possível gerar um código único após {MaxGeneratedCodeAttempts} tentativas. Nenhum link foi criado; tente novamente.");
    }
}
