using LinkShortener.Api.Application;
using LinkShortener.Api.Domain;
using LinkShortener.Tests.Fakes;

namespace LinkShortener.Tests.Unit;

public sealed class LinkResolutionCacheTests
{
    private const string TargetUrl = "https://www.exemplo.com/artigo";

    private static readonly DateTime CreatedAtUtc = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly InMemoryLinkRepository _repository = new();
    private readonly ManualTimeProvider _clock = new(new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero));
    private readonly LinkResolutionCache _cache = TestLinkService.NewCache();

    // Uma instância por requisição, como no contêiner (scoped); o cache é compartilhado (singleton).
    private LinkService NewRequestService() => TestLinkService.Create(_repository, _clock, cache: _cache);

    [Fact]
    public async Task Second_resolution_of_same_code_does_not_query_repository()
    {
        _repository.Seed(new Link("PROMO26", TargetUrl, CreatedAtUtc, null));

        var first = await NewRequestService().ResolveAsync("PROMO26", CancellationToken.None);
        var second = await NewRequestService().ResolveAsync("PROMO26", CancellationToken.None);

        Assert.Equal(TargetUrl, first.Value);
        Assert.Equal(TargetUrl, second.Value);
        Assert.Equal(1, _repository.GetByCodeCalls);
    }

    [Fact]
    public async Task Accesses_served_from_cache_are_still_counted()
    {
        _repository.Seed(new Link("PROMO26", TargetUrl, CreatedAtUtc, null));

        for (var i = 0; i < 10; i++)
        {
            await NewRequestService().ResolveAsync("PROMO26", CancellationToken.None);
        }

        Assert.Equal(10, _repository.AccessCountOf("PROMO26"));
        Assert.Equal(1, _repository.GetByCodeCalls);
    }

    [Fact]
    public async Task Cached_link_stops_redirecting_at_0000_utc_of_day_after_expiration()
    {
        var expiresAtUtc = LinkExpiration.ExpiresAtUtc(new DateOnly(2026, 10, 31));
        _repository.Seed(new Link("PROMO26", TargetUrl, CreatedAtUtc, expiresAtUtc));

        _clock.UtcNow = new DateTimeOffset(2026, 10, 31, 23, 59, 59, TimeSpan.Zero);
        var lastValid = await NewRequestService().ResolveAsync("PROMO26", CancellationToken.None);

        _clock.UtcNow = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
        var expired = await NewRequestService().ResolveAsync("PROMO26", CancellationToken.None);

        Assert.True(lastValid.IsSuccess);
        Assert.Equal(LinkErrorType.Expired, expired.Error?.Type);
        Assert.Equal(1, _repository.GetByCodeCalls);
        Assert.Equal(1, _repository.AccessCountOf("PROMO26"));
    }

    [Fact]
    public async Task Unknown_code_is_not_cached_so_a_link_created_later_resolves()
    {
        var service = NewRequestService();

        var before = await service.ResolveAsync("NOVO1234", CancellationToken.None);
        _repository.Seed(new Link("NOVO1234", TargetUrl, CreatedAtUtc, null));
        var after = await NewRequestService().ResolveAsync("NOVO1234", CancellationToken.None);

        Assert.Equal(LinkErrorType.NotFound, before.Error?.Type);
        Assert.Equal(TargetUrl, after.Value);
    }

    [Fact]
    public async Task Stats_always_read_the_persisted_total_and_ignore_the_cache()
    {
        _repository.Seed(new Link("PROMO26", TargetUrl, CreatedAtUtc, null));
        await NewRequestService().ResolveAsync("PROMO26", CancellationToken.None);
        await NewRequestService().ResolveAsync("PROMO26", CancellationToken.None);
        var callsBeforeStats = _repository.GetByCodeCalls;

        var stats = await NewRequestService().GetStatsAsync("PROMO26", CancellationToken.None);

        Assert.Equal(2, stats.Value?.AccessCount);
        Assert.Equal(callsBeforeStats + 1, _repository.GetByCodeCalls);
    }
}
