using LinkShortener.Api.Application;
using LinkShortener.Api.Endpoints;
using LinkShortener.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LinkShortener.Tests.Unit;

public sealed class ExpirationOnCreateTests
{
    private const string TargetUrl = "https://www.exemplo.com/artigo";

    private readonly InMemoryLinkRepository _repository = new();
    private readonly ManualTimeProvider _clock = new(new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero));

    private LinkService Service =>
        TestLinkService.Create(_repository, _clock);

    [Fact]
    public async Task Current_utc_date_is_accepted()
    {
        var result = await Service.CreateAsync(new CreateLinkCommand(TargetUrl, ExpirationDate: new DateOnly(2026, 10, 19)), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc), result.Value.ExpiresAtUtc);
    }

    [Fact]
    public async Task Current_utc_date_is_accepted_at_the_last_second_of_the_day()
    {
        _clock.UtcNow = new DateTimeOffset(2026, 10, 19, 23, 59, 59, TimeSpan.Zero);

        var result = await Service.CreateAsync(new CreateLinkCommand(TargetUrl, ExpirationDate: new DateOnly(2026, 10, 19)), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Future_date_is_accepted()
    {
        var result = await Service.CreateAsync(new CreateLinkCommand(TargetUrl, ExpirationDate: new DateOnly(2026, 10, 31)), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc), result.Value.ExpiresAtUtc);
    }

    [Fact]
    public async Task Link_without_expiration_date_never_expires()
    {
        var result = await Service.CreateAsync(new CreateLinkCommand(TargetUrl), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.ExpiresAtUtc);
    }

    [Fact]
    public async Task Date_before_current_utc_date_is_rejected_and_creates_nothing()
    {
        var result = await Service.CreateAsync(new CreateLinkCommand(TargetUrl, ExpirationDate: new DateOnly(2026, 10, 18)), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(LinkErrorType.Validation, result.Error.Type);
        Assert.Equal("expiresAtUtc", result.Error.Field);
        Assert.Equal("A data de expiração não pode ser anterior à data atual em UTC.", result.Error.Message);
        Assert.Empty(_repository.Links);
    }

    [Fact]
    public async Task Current_date_is_judged_in_utc_not_in_local_time()
    {
        // 23:30 de 19/10 em Brasília (UTC-3) já é 20/10 em UTC: o dia 19 passou a ser passado.
        _clock.UtcNow = new DateTimeOffset(2026, 10, 19, 23, 30, 0, TimeSpan.FromHours(-3));

        var result = await Service.CreateAsync(new CreateLinkCommand(TargetUrl, ExpirationDate: new DateOnly(2026, 10, 19)), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(LinkErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task Rejected_date_is_translated_into_400_with_problem_details()
    {
        var result = await Service.CreateAsync(new CreateLinkCommand(TargetUrl, ExpirationDate: new DateOnly(2026, 10, 18)), CancellationToken.None);

        var response = LinkErrorResults.ToResult(result.Error!);

        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsAssignableFrom<IStatusCodeHttpResult>(response).StatusCode);
        var problem = Assert.IsAssignableFrom<HttpValidationProblemDetails>(Assert.IsAssignableFrom<IValueHttpResult>(response).Value);
        Assert.Equal(400, problem.Status);
        Assert.Equal("A data de expiração não pode ser anterior à data atual em UTC.", problem.Detail);
        Assert.Equal(["A data de expiração não pode ser anterior à data atual em UTC."], problem.Errors["expiresAtUtc"]);
    }
}
