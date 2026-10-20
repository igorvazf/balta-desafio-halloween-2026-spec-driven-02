using System.Net;
using System.Net.Http.Json;
using LinkShortener.Api.Endpoints;

namespace LinkShortener.Tests.Integration;

public sealed class ResolutionCacheTests : IntegrationTest
{
    [Fact]
    public async Task N_consecutive_accesses_result_in_counter_N()
    {
        const int accesses = 25;
        var link = await CreateLinkAsync(new { targetUrl = TargetUrl });

        for (var i = 0; i < accesses; i++)
        {
            var response = await Client.GetAsync($"/{link.Code}");
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        }

        var stats = await Client.GetFromJsonAsync<LinkStatsResponse>($"/api/links/{link.Code}/stats");
        Assert.Equal(accesses, stats!.AccessCount);
    }

    [Fact]
    public async Task Cached_link_stops_redirecting_from_0000_utc_of_day_after_expiration()
    {
        var link = await CreateLinkAsync(new { targetUrl = TargetUrl, expiresAtUtc = "2026-10-31" });

        // Acessos durante a validade colocam o link no cache.
        Clock.UtcNow = new DateTimeOffset(2026, 10, 31, 12, 0, 0, TimeSpan.Zero);
        Assert.Equal(HttpStatusCode.Found, (await Client.GetAsync($"/{link.Code}")).StatusCode);
        Clock.UtcNow = new DateTimeOffset(2026, 10, 31, 23, 59, 59, TimeSpan.Zero);
        Assert.Equal(HttpStatusCode.Found, (await Client.GetAsync($"/{link.Code}")).StatusCode);

        Clock.UtcNow = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
        var response = await Client.GetAsync($"/{link.Code}");

        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
        var stats = await Client.GetFromJsonAsync<LinkStatsResponse>($"/api/links/{link.Code}/stats");
        Assert.Equal(2, stats!.AccessCount);
    }
}
