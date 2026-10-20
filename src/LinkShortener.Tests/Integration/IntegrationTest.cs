using System.Net.Http.Json;
using System.Text.Json;
using LinkShortener.Api.Endpoints;
using LinkShortener.Tests.Fakes;

namespace LinkShortener.Tests.Integration;

/// <summary>
/// Cada teste ganha a própria aplicação, o próprio arquivo SQLite e o próprio relógio.
/// </summary>
public abstract class IntegrationTest : IAsyncLifetime
{
    protected const string TargetUrl = "https://www.exemplo.com/artigos/2026/10/encurtador-de-links";

    protected static readonly DateTimeOffset Today = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

    protected TemporaryDatabase Database { get; } = new();

    protected ManualTimeProvider Clock { get; } = new(Today);

    protected FakeHostAddressResolver HostAddressResolver { get; } = new();

    protected TestApplication App { get; private set; } = null!;

    protected HttpClient Client => App.Client;

    public async Task InitializeAsync() => App = await StartApplicationAsync();

    public async Task DisposeAsync()
    {
        await App.DisposeAsync();
        Database.Dispose();
    }

    protected Task<TestApplication> StartApplicationAsync() =>
        TestApplication.StartAsync(Database.ConnectionString, Clock, HostAddressResolver);

    /// <summary>
    /// Derruba a aplicação, solta o arquivo SQLite e sobe uma nova instância (sem cache) sobre o mesmo arquivo.
    /// </summary>
    protected async Task RestartApplicationAsync()
    {
        await App.DisposeAsync();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        App = await StartApplicationAsync();
    }

    protected Task<HttpResponseMessage> PostLinkAsync(object body) => Client.PostAsJsonAsync("/api/links", body);

    protected async Task<CreateLinkResponse> CreateLinkAsync(object body)
    {
        var response = await PostLinkAsync(body);
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<CreateLinkResponse>())!;
    }

    protected static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)response.StatusCode, problem.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("title").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("detail").GetString()));

        return problem;
    }
}
