using System.Net;
using System.Net.Http.Json;
using System.Text;
using LinkShortener.Api.Endpoints;

namespace LinkShortener.Tests.Integration;

public sealed class CreateLinkTests : IntegrationTest
{
    [Fact]
    public async Task Creating_link_with_automatic_code_returns_201_with_short_url()
    {
        var response = await PostLinkAsync(new { targetUrl = TargetUrl });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CreateLinkResponse>();
        Assert.NotNull(body);
        Assert.Matches("^[A-Z1-9]{7}$", body.Code);
        Assert.Equal(new Uri(Client.BaseAddress!, body.Code).ToString(), body.ShortUrl);
        Assert.Equal(body.ShortUrl, response.Headers.Location?.ToString());
    }

    [Theory]
    [InlineData("ftp://www.exemplo.com/arquivo")]
    [InlineData("nao-e-uma-url")]
    [InlineData("http://127.0.0.1/admin")]
    [InlineData("https://nao-existe.exemplo.com/")]
    public async Task Invalid_target_url_returns_400_with_problem_details(string targetUrl)
    {
        var response = await PostLinkAsync(new { targetUrl });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadProblemAsync(response);
        Assert.True(problem.GetProperty("errors").TryGetProperty("targetUrl", out _));
    }

    [Fact]
    public async Task Target_url_longer_than_2048_characters_returns_400_with_problem_details()
    {
        var response = await PostLinkAsync(new { targetUrl = "https://www.exemplo.com/" + new string('a', 2048) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await ReadProblemAsync(response);
    }

    [Fact]
    public async Task Missing_target_url_returns_400_with_problem_details()
    {
        var response = await PostLinkAsync(new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadProblemAsync(response);
        Assert.Equal("O campo targetUrl é obrigatório.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Malformed_body_returns_400_with_problem_details()
    {
        var response = await Client.PostAsync("/api/links", new StringContent("{ targetUrl: ", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await ReadProblemAsync(response);
    }
}
