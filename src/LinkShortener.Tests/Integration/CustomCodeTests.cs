using System.Net;

namespace LinkShortener.Tests.Integration;

public sealed class CustomCodeTests : IntegrationTest
{
    [Fact]
    public async Task Creating_link_with_custom_code_returns_201_with_that_code()
    {
        var body = await CreateLinkAsync(new { targetUrl = TargetUrl, code = "PROMO26" });

        Assert.Equal("PROMO26", body.Code);
        Assert.EndsWith("/PROMO26", body.ShortUrl);
    }

    [Fact]
    public async Task Duplicate_code_returns_409_with_problem_details()
    {
        await CreateLinkAsync(new { targetUrl = TargetUrl, code = "PROMO26" });

        var response = await PostLinkAsync(new { targetUrl = "https://www.exemplo.com/outro", code = "PROMO26" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var problem = await ReadProblemAsync(response);
        Assert.Equal("O código 'PROMO26' já está em uso. Escolha outro código.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Concurrent_creations_with_same_code_create_only_one_link()
    {
        var responses = await Task.WhenAll(Enumerable.Range(0, 10)
            .Select(_ => PostLinkAsync(new { targetUrl = TargetUrl, code = "CORRIDA" })));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.All(responses.Where(r => r.StatusCode != HttpStatusCode.Created),
            r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("promo26")]
    [InlineData("PROMO2026")]
    public async Task Invalid_custom_code_returns_400_with_problem_details(string code)
    {
        var response = await PostLinkAsync(new { targetUrl = TargetUrl, code });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadProblemAsync(response);
        Assert.True(problem.GetProperty("errors").TryGetProperty("code", out _));
    }
}
