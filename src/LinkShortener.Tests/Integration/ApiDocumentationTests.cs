using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace LinkShortener.Tests.Integration;

/// <summary>
/// A aplicação de teste roda no ambiente Production, fora do ambiente de desenvolvimento.
/// </summary>
public sealed class ApiDocumentationTests : IntegrationTest
{
    [Theory]
    [InlineData("/api/docs")]
    [InlineData("/api/docs/")]
    [InlineData("/api/docs/index.html")]
    public async Task Docs_page_responds_200_outside_development(string path)
    {
        var response = await Client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<base href=\"/api/docs/\">", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/api/docs/swagger-ui.css")]
    [InlineData("/api/docs/swagger-ui-bundle.js")]
    [InlineData("/api/docs/index.js")]
    public async Task Docs_page_assets_are_served_under_docs_prefix(string path)
    {
        var response = await Client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OpenApi_document_lists_the_three_endpoints_with_all_success_and_error_responses()
    {
        var document = await Client.GetFromJsonAsync<JsonElement>("/api/docs/v1/swagger.json");
        var paths = document.GetProperty("paths");

        Assert.Equal(
            new[] { "/api/links", "/api/links/{code}/stats", "/{code}" },
            paths.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal).ToArray());

        Assert.Equal(new[] { "201", "400", "409", "503" }, ResponseCodes(paths, "/api/links", "post"));
        Assert.Equal(new[] { "200", "404" }, ResponseCodes(paths, "/api/links/{code}/stats", "get"));
        Assert.Equal(new[] { "302", "404", "410" }, ResponseCodes(paths, "/{code}", "get"));
    }

    [Fact]
    public async Task OpenApi_document_describes_error_responses_as_problem_details()
    {
        var document = await Client.GetFromJsonAsync<JsonElement>("/api/docs/v1/swagger.json");
        var paths = document.GetProperty("paths");

        foreach (var (path, method, status) in new[]
        {
            ("/api/links", "post", "400"), ("/api/links", "post", "409"), ("/api/links", "post", "503"),
            ("/api/links/{code}/stats", "get", "404"),
            ("/{code}", "get", "404"), ("/{code}", "get", "410"),
        })
        {
            var response = paths.GetProperty(path).GetProperty(method).GetProperty("responses").GetProperty(status);
            Assert.True(response.GetProperty("content").TryGetProperty("application/problem+json", out _), $"{method} {path} {status}");
            Assert.False(string.IsNullOrWhiteSpace(response.GetProperty("description").GetString()));
        }
    }

    [Fact]
    public async Task OpenApi_document_has_input_examples_and_redirect_location_header()
    {
        var document = await Client.GetFromJsonAsync<JsonElement>("/api/docs/v1/swagger.json");

        var requestExample = document.GetProperty("components").GetProperty("schemas")
            .GetProperty("CreateLinkRequest").GetProperty("example");
        Assert.StartsWith("https://", requestExample.GetProperty("targetUrl").GetString());
        Assert.Equal("PROMO26", requestExample.GetProperty("code").GetString());
        Assert.Equal("2026-10-31", requestExample.GetProperty("expiresAtUtc").GetString());

        var redirect = document.GetProperty("paths").GetProperty("/{code}").GetProperty("get");
        var codeParameter = redirect.GetProperty("parameters").EnumerateArray().Single(p => p.GetProperty("name").GetString() == "code");
        Assert.Equal("PROMO26", codeParameter.GetProperty("example").GetString());
        Assert.True(redirect.GetProperty("responses").GetProperty("302").GetProperty("headers").TryGetProperty("Location", out _));
    }

    [Fact]
    public async Task Link_with_custom_code_SWAGGER_redirects_normally()
    {
        await CreateLinkAsync(new { targetUrl = TargetUrl, code = "SWAGGER" });

        var response = await Client.GetAsync("/SWAGGER");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal(TargetUrl, response.Headers.Location?.ToString());
    }

    private static string[] ResponseCodes(JsonElement paths, string path, string method) =>
        paths.GetProperty(path).GetProperty(method).GetProperty("responses")
            .EnumerateObject().Select(r => r.Name).Order(StringComparer.Ordinal).ToArray();
}
