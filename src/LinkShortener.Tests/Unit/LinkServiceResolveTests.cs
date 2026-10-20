using LinkShortener.Api.Application;
using LinkShortener.Api.Domain;
using LinkShortener.Tests.Fakes;

namespace LinkShortener.Tests.Unit;

public sealed class LinkServiceResolveTests
{
    private const string TargetUrl = "https://www.exemplo.com/artigo";

    private static readonly DateTimeOffset Now = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryLinkRepository _repository = new();
    private readonly ManualTimeProvider _clock = new(Now);

    private LinkService CreateService() =>
        TestLinkService.Create(_repository, _clock);

    [Fact]
    public async Task Resolving_existing_code_returns_target_url()
    {
        _repository.Seed(new Link("PROMO26", TargetUrl, Now.UtcDateTime, null));

        var result = await CreateService().ResolveAsync("PROMO26", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TargetUrl, result.Value);
    }

    [Fact]
    public async Task Resolving_unknown_code_is_not_found()
    {
        var result = await CreateService().ResolveAsync("NAOEXISTE", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(LinkErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Each_valid_access_increments_counter_by_one_and_never_decreases()
    {
        _repository.Seed(new Link("PROMO26", TargetUrl, Now.UtcDateTime, null));
        var service = CreateService();

        var previous = _repository.AccessCountOf("PROMO26");
        for (var i = 1; i <= 5; i++)
        {
            await service.ResolveAsync("PROMO26", CancellationToken.None);

            var current = _repository.AccessCountOf("PROMO26");
            Assert.Equal(previous + 1, current);
            previous = current;
        }
    }

    [Fact]
    public async Task Repeated_accesses_from_new_service_instances_are_all_counted()
    {
        _repository.Seed(new Link("PROMO26", TargetUrl, Now.UtcDateTime, null));

        for (var i = 0; i < 3; i++)
        {
            await CreateService().ResolveAsync("PROMO26", CancellationToken.None);
        }

        Assert.Equal(3, _repository.AccessCountOf("PROMO26"));
    }
}
