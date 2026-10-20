using System.Net;
using System.Net.Http.Json;
using LinkShortener.Api.Endpoints;

namespace LinkShortener.Tests.Integration;

public sealed class PersistenceTests : IntegrationTest
{
    [Fact]
    public async Task Link_keeps_redirecting_after_application_restart_over_same_sqlite_file()
    {
        var link = await CreateLinkAsync(new { targetUrl = TargetUrl, code = "PERSISTE" });
        await Client.GetAsync($"/{link.Code}");

        await RestartApplicationAsync();

        var response = await Client.GetAsync($"/{link.Code}");
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal(TargetUrl, response.Headers.Location?.ToString());

        var stats = await Client.GetFromJsonAsync<LinkStatsResponse>($"/api/links/{link.Code}/stats");
        Assert.Equal(2, stats!.AccessCount);
    }

    [Fact]
    public async Task Code_stays_taken_after_application_restart()
    {
        await CreateLinkAsync(new { targetUrl = TargetUrl, code = "PERSISTE" });

        await RestartApplicationAsync();

        var response = await PostLinkAsync(new { targetUrl = TargetUrl, code = "PERSISTE" });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Expiration_is_kept_after_application_restart()
    {
        var link = await CreateLinkAsync(new { targetUrl = TargetUrl, expiresAtUtc = "2026-10-31" });

        await RestartApplicationAsync();

        Clock.UtcNow = new DateTimeOffset(2026, 10, 31, 23, 59, 59, TimeSpan.Zero);
        Assert.Equal(HttpStatusCode.Found, (await Client.GetAsync($"/{link.Code}")).StatusCode);
        Clock.UtcNow = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
        Assert.Equal(HttpStatusCode.Gone, (await Client.GetAsync($"/{link.Code}")).StatusCode);
    }
}
