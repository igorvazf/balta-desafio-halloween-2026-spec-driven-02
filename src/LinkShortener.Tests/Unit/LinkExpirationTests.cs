using LinkShortener.Api.Application;
using LinkShortener.Api.Domain;
using LinkShortener.Tests.Fakes;

namespace LinkShortener.Tests.Unit;

public sealed class LinkExpirationTests
{
    private static readonly DateOnly ExpirationDate = new(2026, 10, 31);
    private static readonly DateTime ExpiresAtUtc = LinkExpiration.ExpiresAtUtc(ExpirationDate);

    [Fact]
    public void Link_is_valid_at_235959_utc_of_expiration_date()
    {
        Assert.False(LinkExpiration.IsExpired(ExpiresAtUtc, new DateTimeOffset(2026, 10, 31, 23, 59, 59, TimeSpan.Zero)));
    }

    [Fact]
    public void Link_is_valid_at_the_last_tick_of_expiration_date()
    {
        var lastTick = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero).AddTicks(-1);

        Assert.False(LinkExpiration.IsExpired(ExpiresAtUtc, lastTick));
    }

    [Fact]
    public void Link_is_expired_at_0000_utc_of_the_next_day()
    {
        Assert.True(LinkExpiration.IsExpired(ExpiresAtUtc, new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void Expiration_is_judged_in_utc_whatever_the_clock_offset()
    {
        // 21:00 de 31/10 em Brasília (UTC-3) já é 00:00 de 01/11 em UTC.
        Assert.True(LinkExpiration.IsExpired(ExpiresAtUtc, new DateTimeOffset(2026, 10, 31, 21, 0, 0, TimeSpan.FromHours(-3))));
    }

    [Fact]
    public void Link_without_expiration_never_expires()
    {
        Assert.False(LinkExpiration.IsExpired(null, DateTimeOffset.MaxValue));
    }

    [Fact]
    public async Task Expired_link_does_not_redirect_and_does_not_count_access()
    {
        var repository = new InMemoryLinkRepository();
        var clock = new ManualTimeProvider(new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero));
        repository.Seed(new Link("VENCIDO", "https://www.exemplo.com/", new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), ExpiresAtUtc));
        var service = TestLinkService.Create(repository, clock);

        var result = await service.ResolveAsync("VENCIDO", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(LinkErrorType.Expired, result.Error.Type);
        Assert.Equal(0, repository.AccessCountOf("VENCIDO"));
    }

    [Fact]
    public async Task Expired_link_still_reports_its_total()
    {
        var repository = new InMemoryLinkRepository();
        var clock = new ManualTimeProvider(new DateTimeOffset(2026, 10, 31, 12, 0, 0, TimeSpan.Zero));
        repository.Seed(new Link("VENCIDO", "https://www.exemplo.com/", new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), ExpiresAtUtc));
        var service = TestLinkService.Create(repository, clock);
        await service.ResolveAsync("VENCIDO", CancellationToken.None);

        clock.UtcNow = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
        var result = await service.GetStatsAsync("VENCIDO", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new LinkStats("VENCIDO", 1), result.Value);
    }
}
