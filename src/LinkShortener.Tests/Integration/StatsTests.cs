using System.Net;
using System.Net.Http.Json;
using LinkShortener.Api.Data;
using LinkShortener.Api.Domain;
using LinkShortener.Api.Endpoints;
using Microsoft.EntityFrameworkCore;

namespace LinkShortener.Tests.Integration;

public sealed class StatsTests : IntegrationTest
{
    [Fact]
    public async Task Returns_200_with_access_total_without_requiring_any_header()
    {
        var link = await CreateLinkAsync(new { targetUrl = TargetUrl });
        await Client.GetAsync($"/{link.Code}");
        await Client.GetAsync($"/{link.Code}");
        await Client.GetAsync($"/{link.Code}");

        using var anonymous = new HttpClient { BaseAddress = Client.BaseAddress };
        var response = await anonymous.GetAsync($"/api/links/{link.Code}/stats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stats = await response.Content.ReadFromJsonAsync<LinkStatsResponse>();
        Assert.Equal(new LinkStatsResponse(link.Code, 3), stats);
    }

    [Fact]
    public async Task New_link_has_zero_accesses()
    {
        var link = await CreateLinkAsync(new { targetUrl = TargetUrl });

        var stats = await Client.GetFromJsonAsync<LinkStatsResponse>($"/api/links/{link.Code}/stats");

        Assert.Equal(0, stats!.AccessCount);
    }

    [Fact]
    public async Task Unknown_code_returns_404_with_problem_details()
    {
        var response = await Client.GetAsync("/api/links/NAOEXISTE/stats");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await ReadProblemAsync(response);
    }

    [Fact]
    public async Task Expired_link_still_returns_its_total()
    {
        await using (var context = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseSqlite(Database.ConnectionString).Options))
        {
            // Link que expirou ontem, com 7 acessos feitos enquanto era válido.
            var expiredAt = Today.UtcDateTime.Date;
            context.Links.Add(new Link("VENCIDO", TargetUrl, expiredAt.AddDays(-10), expiredAt));
            await context.SaveChangesAsync();
            await context.Links.Where(l => l.Code == "VENCIDO")
                .ExecuteUpdateAsync(set => set.SetProperty(l => l.AccessCount, 7));
        }

        var response = await Client.GetAsync("/api/links/VENCIDO/stats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new LinkStatsResponse("VENCIDO", 7), await response.Content.ReadFromJsonAsync<LinkStatsResponse>());
    }
}
