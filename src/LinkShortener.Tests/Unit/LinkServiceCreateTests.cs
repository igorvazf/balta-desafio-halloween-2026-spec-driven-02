using LinkShortener.Api.Application;
using LinkShortener.Api.Domain;
using LinkShortener.Tests.Fakes;

namespace LinkShortener.Tests.Unit;

public sealed class LinkServiceCreateTests
{
    private const string TargetUrl = "https://www.exemplo.com/artigo";

    private static readonly DateTimeOffset Now = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryLinkRepository _repository = new();
    private readonly ManualTimeProvider _clock = new(Now);
    private ICodeGenerator _codeGenerator = new SequenceCodeGenerator("AUTO123");

    private LinkService Service => TestLinkService.Create(_repository, _clock, _codeGenerator);

    [Fact]
    public async Task Creates_link_with_generated_code()
    {
        var result = await Service.CreateAsync(new CreateLinkCommand(TargetUrl), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("AUTO123", result.Value.Code);
        Assert.Equal(TargetUrl, result.Value.TargetUrl);
        Assert.Equal(Now.UtcDateTime, result.Value.CreatedAtUtc);
        Assert.Equal(0, result.Value.AccessCount);
        Assert.Single(_repository.Links);
    }

    [Fact]
    public async Task Invalid_target_url_is_a_validation_error_and_creates_nothing()
    {
        var result = await Service.CreateAsync(new CreateLinkCommand("ftp://www.exemplo.com"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(LinkErrorType.Validation, result.Error.Type);
        Assert.Equal("targetUrl", result.Error.Field);
        Assert.Empty(_repository.Links);
    }

    [Fact]
    public async Task Generated_code_in_use_generates_another_code()
    {
        _repository.Seed(new Link("AAAAAAA", "https://www.exemplo.com/existente", Now.UtcDateTime, null));
        var generator = new SequenceCodeGenerator("AAAAAAA", "BBBBBBB");
        _codeGenerator = generator;

        var result = await Service.CreateAsync(new CreateLinkCommand(TargetUrl), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("BBBBBBB", result.Value.Code);
        Assert.Equal(2, generator.Calls);
    }

    [Fact]
    public async Task Generated_code_taken_between_check_and_insert_generates_another_code()
    {
        _repository.CodesTakenConcurrently.Add("AAAAAAA");
        _codeGenerator = new SequenceCodeGenerator("AAAAAAA", "BBBBBBB");

        var result = await Service.CreateAsync(new CreateLinkCommand(TargetUrl), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("BBBBBBB", result.Value.Code);
    }

    [Fact]
    public async Task Succeeds_when_the_fifth_generated_code_is_free()
    {
        _repository.Seed(new Link("AAAAAAA", TargetUrl, Now.UtcDateTime, null));
        _codeGenerator = new SequenceCodeGenerator("AAAAAAA", "AAAAAAA", "AAAAAAA", "AAAAAAA", "EEEEEEE");

        var result = await Service.CreateAsync(new CreateLinkCommand(TargetUrl), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("EEEEEEE", result.Value.Code);
    }

    [Fact]
    public async Task Five_collisions_fail_the_creation_and_create_no_link()
    {
        _repository.Seed(new Link("AAAAAAA", "https://www.exemplo.com/existente", Now.UtcDateTime, null));
        var generator = new SequenceCodeGenerator("AAAAAAA");
        _codeGenerator = generator;

        var result = await Service.CreateAsync(new CreateLinkCommand(TargetUrl), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(LinkErrorType.CodeGenerationFailed, result.Error.Type);
        Assert.Equal(5, generator.Calls);
        Assert.Equal("https://www.exemplo.com/existente", Assert.Single(_repository.Links).TargetUrl);
    }

    [Fact]
    public async Task Five_collisions_at_insert_time_fail_the_creation_and_create_no_link()
    {
        _repository.CodesTakenConcurrently.Add("AAAAAAA");
        var generator = new SequenceCodeGenerator("AAAAAAA");
        _codeGenerator = generator;

        var result = await Service.CreateAsync(new CreateLinkCommand(TargetUrl), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(LinkErrorType.CodeGenerationFailed, result.Error.Type);
        Assert.Equal(5, generator.Calls);
        Assert.Empty(_repository.Links);
    }

    [Fact]
    public async Task Creates_link_with_custom_code()
    {
        var result = await Service.CreateAsync(new CreateLinkCommand(TargetUrl, "PROMO26"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("PROMO26", result.Value.Code);
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("ABCDEFGHIJKLMNOPQRS12")]
    [InlineData("promo26")]
    [InlineData("PROMO2026")]
    [InlineData("PRO MO")]
    [InlineData("PROMO!")]
    public async Task Invalid_custom_code_is_a_validation_error_and_creates_nothing(string code)
    {
        var result = await Service.CreateAsync(new CreateLinkCommand(TargetUrl, code), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(LinkErrorType.Validation, result.Error.Type);
        Assert.Equal("code", result.Error.Field);
        Assert.Empty(_repository.Links);
    }

    [Fact]
    public async Task Custom_code_already_in_use_is_a_conflict_and_keeps_the_existing_link()
    {
        _repository.Seed(new Link("PROMO26", "https://www.exemplo.com/original", Now.UtcDateTime, null));

        var result = await Service.CreateAsync(new CreateLinkCommand(TargetUrl, "PROMO26"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(LinkErrorType.Conflict, result.Error.Type);
        Assert.Equal("O código 'PROMO26' já está em uso. Escolha outro código.", result.Error.Message);
        Assert.Equal("https://www.exemplo.com/original", Assert.Single(_repository.Links).TargetUrl);
    }

    [Fact]
    public async Task Custom_code_taken_between_check_and_insert_is_a_conflict()
    {
        _repository.CodesTakenConcurrently.Add("PROMO26");

        var result = await Service.CreateAsync(new CreateLinkCommand(TargetUrl, "PROMO26"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(LinkErrorType.Conflict, result.Error.Type);
        Assert.Empty(_repository.Links);
    }
}
