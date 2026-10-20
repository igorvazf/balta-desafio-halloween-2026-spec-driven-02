using System.Net;

namespace LinkShortener.Tests.Integration;

public sealed class RedirectTests : IntegrationTest
{
    [Fact]
    public async Task Existing_code_returns_302_with_location_header()
    {
        var link = await CreateLinkAsync(new { targetUrl = TargetUrl });

        var response = await Client.GetAsync($"/{link.Code}");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal(TargetUrl, response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Short_url_returned_on_creation_redirects()
    {
        var link = await CreateLinkAsync(new { targetUrl = TargetUrl, code = "PROMO26" });

        var response = await Client.GetAsync(link.ShortUrl);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal(TargetUrl, response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Unknown_code_returns_404_with_problem_details()
    {
        var response = await Client.GetAsync("/NAOEXISTE");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await ReadProblemAsync(response);
        Assert.Equal("O link com o código 'NAOEXISTE' não existe.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Code_lookup_is_case_sensitive()
    {
        await CreateLinkAsync(new { targetUrl = TargetUrl, code = "PROMO26" });

        var response = await Client.GetAsync("/promo26");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
