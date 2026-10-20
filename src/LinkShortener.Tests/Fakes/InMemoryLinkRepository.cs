using System.Reflection;
using LinkShortener.Api.Application;
using LinkShortener.Api.Domain;

namespace LinkShortener.Tests.Fakes;

/// <summary>
/// Repositório em memória que conta as chamadas, para os testes unitários do serviço.
/// </summary>
public sealed class InMemoryLinkRepository : ILinkRepository
{
    private static readonly PropertyInfo IdProperty = typeof(Link).GetProperty(nameof(Link.Id))!;
    private static readonly PropertyInfo AccessCountProperty = typeof(Link).GetProperty(nameof(Link.AccessCount))!;

    private readonly Dictionary<string, Link> _links = new(StringComparer.Ordinal);

    public IReadOnlyCollection<Link> Links => _links.Values;

    public int GetByCodeCalls { get; private set; }

    public int TryAddCalls { get; private set; }

    /// <summary>
    /// Códigos que o "banco" recusa no TryAdd, simulando outra criação que gravou o mesmo código antes.
    /// </summary>
    public HashSet<string> CodesTakenConcurrently { get; } = new(StringComparer.Ordinal);

    public long AccessCountOf(string code) => _links[code].AccessCount;

    public Link Seed(Link link)
    {
        IdProperty.SetValue(link, _links.Count + 1);
        _links[link.Code] = link;
        return link;
    }

    public Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(_links.ContainsKey(code));

    public Task<bool> TryAddAsync(Link link, CancellationToken cancellationToken)
    {
        TryAddCalls++;

        if (_links.ContainsKey(link.Code) || CodesTakenConcurrently.Contains(link.Code))
        {
            return Task.FromResult(false);
        }

        Seed(link);
        return Task.FromResult(true);
    }

    public Task<Link?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        GetByCodeCalls++;
        return Task.FromResult(_links.GetValueOrDefault(code));
    }

    public Task IncrementAccessCountAsync(int linkId, CancellationToken cancellationToken)
    {
        var link = _links.Values.Single(l => l.Id == linkId);
        AccessCountProperty.SetValue(link, link.AccessCount + 1);
        return Task.CompletedTask;
    }
}
