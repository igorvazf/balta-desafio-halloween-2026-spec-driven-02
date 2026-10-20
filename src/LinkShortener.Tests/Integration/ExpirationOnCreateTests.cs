using System.Net;

namespace LinkShortener.Tests.Integration;

public sealed class ExpirationOnCreateTests : IntegrationTest
{
    [Fact]
    public async Task Current_date_is_accepted()
    {
        var response = await PostLinkAsync(new { targetUrl = TargetUrl, expiresAtUtc = "2026-10-19" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Past_date_returns_400_with_problem_details()
    {
        var response = await PostLinkAsync(new { targetUrl = TargetUrl, expiresAtUtc = "2026-10-18" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadProblemAsync(response);
        Assert.True(problem.GetProperty("errors").TryGetProperty("expiresAtUtc", out _));
    }

    [Fact]
    public async Task Date_with_time_returns_400_with_problem_details()
    {
        var response = await PostLinkAsync(new { targetUrl = TargetUrl, expiresAtUtc = "2026-10-31T10:00:00Z" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await ReadProblemAsync(response);
    }
}
