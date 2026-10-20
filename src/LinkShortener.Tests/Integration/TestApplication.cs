using LinkShortener.Api;
using LinkShortener.Api.Application;
using LinkShortener.Tests.Fakes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LinkShortener.Tests.Integration;

/// <summary>
/// Sobe a aplicação real no Kestrel, numa porta livre do loopback, fora do ambiente de desenvolvimento.
/// </summary>
public sealed class TestApplication : IAsyncDisposable
{
    private readonly WebApplication _app;

    private TestApplication(WebApplication app, HttpClient client)
    {
        _app = app;
        Client = client;
    }

    public HttpClient Client { get; }

    public static async Task<TestApplication> StartAsync(
        string connectionString,
        TimeProvider clock,
        IHostAddressResolver hostAddressResolver)
    {
        var options = new WebApplicationOptions
        {
            EnvironmentName = Environments.Production,
            ApplicationName = typeof(ApiApplication).Assembly.GetName().Name,
            ContentRootPath = AppContext.BaseDirectory,
        };

        var app = ApiApplication.Build(options, builder =>
        {
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Logging.ClearProviders();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = connectionString,
            });
            builder.Services.AddSingleton(clock);
            builder.Services.AddSingleton(hostAddressResolver);
        });

        await app.StartAsync();

        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = new Uri(address) };

        return new TestApplication(app, client);
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
