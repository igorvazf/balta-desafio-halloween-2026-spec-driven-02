using System.Net;
using LinkShortener.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace LinkShortener.Tests.Integration;

public sealed class AccessCounterTests : IntegrationTest
{
    [Fact]
    public async Task Concurrent_accesses_do_not_lose_counts()
    {
        const int accesses = 50;
        var link = await CreateLinkAsync(new { targetUrl = TargetUrl });

        var responses = await Task.WhenAll(Enumerable.Range(0, accesses).Select(_ => Client.GetAsync($"/{link.Code}")));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Found, r.StatusCode));
        Assert.Equal(accesses, await ReadAccessCountAsync(link.Code));
    }

    [Fact]
    public async Task Repository_increment_is_atomic_across_concurrent_contexts()
    {
        const int workers = 8;
        const int incrementsPerWorker = 25;
        var link = await CreateLinkAsync(new { targetUrl = TargetUrl });
        var linkId = await ReadLinkIdAsync(link.Code);

        await Task.WhenAll(Enumerable.Range(0, workers).Select(_ => Task.Run(async () =>
        {
            await using var context = CreateContext();
            var repository = new LinkRepository(context);
            for (var i = 0; i < incrementsPerWorker; i++)
            {
                await repository.IncrementAccessCountAsync(linkId, CancellationToken.None);
            }
        })));

        Assert.Equal(workers * incrementsPerWorker, await ReadAccessCountAsync(link.Code));
    }

    [Fact]
    public async Task Unknown_code_does_not_change_any_counter()
    {
        var link = await CreateLinkAsync(new { targetUrl = TargetUrl });

        await Client.GetAsync("/NAOEXISTE");

        Assert.Equal(0, await ReadAccessCountAsync(link.Code));
    }

    private AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(Database.ConnectionString).Options);

    private async Task<int> ReadLinkIdAsync(string code)
    {
        await using var context = CreateContext();
        return await context.Links.Where(l => l.Code == code).Select(l => l.Id).SingleAsync();
    }

    private async Task<long> ReadAccessCountAsync(string code)
    {
        await using var context = CreateContext();
        return await context.Links.Where(l => l.Code == code).Select(l => l.AccessCount).SingleAsync();
    }
}
