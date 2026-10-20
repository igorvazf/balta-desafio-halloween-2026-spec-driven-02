using System.Net;
using System.Net.Http.Json;
using LinkShortener.Api.Endpoints;

namespace LinkShortener.Tests.Integration;

public sealed class ExpiredLinkTests : IntegrationTest
{
    [Fact]
    public async Task Link_is_valid_until_235959_utc_and_returns_410_from_0000_utc_of_next_day_without_counting()
    {
        var link = await CreateLinkAsync(new { targetUrl = TargetUrl, expiresAtUtc = "2026-10-31" });

        Clock.UtcNow = new DateTimeOffset(2026, 10, 31, 23, 59, 59, TimeSpan.Zero);
        var lastValidAccess = await Client.GetAsync($"/{link.Code}");

        Assert.Equal(HttpStatusCode.Found, lastValidAccess.StatusCode);
        Assert.Equal(TargetUrl, lastValidAccess.Headers.Location?.ToString());
        Assert.Equal(1, await AccessCountAsync(link.Code));

        Clock.UtcNow = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
        var expiredAccess = await Client.GetAsync($"/{link.Code}");

        Assert.Equal(HttpStatusCode.Gone, expiredAccess.StatusCode);
        Assert.Null(expiredAccess.Headers.Location);
        await ReadProblemAsync(expiredAccess);
        Assert.Equal(1, await AccessCountAsync(link.Code));
    }

    [Fact]
    public async Task Repeated_accesses_to_expired_link_never_change_the_counter()
    {
        var link = await CreateLinkAsync(new { targetUrl = TargetUrl, expiresAtUtc = "2026-10-19" });
        Clock.UtcNow = new DateTimeOffset(2026, 10, 20, 0, 0, 0, TimeSpan.Zero);

        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(HttpStatusCode.Gone, (await Client.GetAsync($"/{link.Code}")).StatusCode);
        }

        Assert.Equal(0, await AccessCountAsync(link.Code));
    }

    private async Task<long> AccessCountAsync(string code) =>
        (await Client.GetFromJsonAsync<LinkStatsResponse>($"/api/links/{code}/stats"))!.AccessCount;
}
