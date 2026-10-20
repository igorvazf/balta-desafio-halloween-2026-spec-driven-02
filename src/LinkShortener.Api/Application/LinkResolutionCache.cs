using LinkShortener.Api.Domain;
using Microsoft.Extensions.Caching.Memory;

namespace LinkShortener.Api.Application;

/// <summary>
/// Dados de um link que o redirecionamento precisa. Guarda a expiração para que a RN-03
/// continue sendo avaliada a cada acesso, mesmo com o destino vindo do cache.
/// </summary>
public sealed record ResolvedLink(int Id, string TargetUrl, DateTime? ExpiresAtUtc)
{
    public static ResolvedLink From(Link link) => new(link.Id, link.TargetUrl, link.ExpiresAtUtc);
}

/// <summary>
/// Cache em memória dos códigos acessados recentemente (RNF-01). Guarda só o destino e a expiração,
/// que não mudam depois da criação; o contador nunca passa por aqui.
/// </summary>
public sealed class LinkResolutionCache(IMemoryCache cache)
{
    public const int MaxEntries = 10_000;

    private static readonly TimeSpan SlidingExpiration = TimeSpan.FromMinutes(10);

    public bool TryGet(string code, out ResolvedLink link) =>
        cache.TryGetValue(Key(code), out link!);

    public void Set(string code, ResolvedLink link) =>
        cache.Set(Key(code), link, new MemoryCacheEntryOptions { Size = 1, SlidingExpiration = SlidingExpiration });

    private static string Key(string code) => $"link:{code}";
}
