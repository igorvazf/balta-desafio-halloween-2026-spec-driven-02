using LinkShortener.Api.Application;
using LinkShortener.Tests.Fakes;

namespace LinkShortener.Tests.Unit;

public sealed class TargetUrlValidatorTests
{
    private readonly FakeHostAddressResolver _resolver = new();

    private TargetUrlValidator Validator => new(_resolver);

    [Theory]
    [InlineData("http://www.exemplo.com/artigo")]
    [InlineData("https://www.exemplo.com/artigo?x=1")]
    [InlineData("HTTPS://WWW.EXEMPLO.COM")]
    public async Task Accepts_http_and_https(string url)
    {
        var result = await Validator.ValidateAsync(url, CancellationToken.None);

        Assert.True(result.IsValid, result.Error);
    }

    [Theory]
    [InlineData("ftp://www.exemplo.com/arquivo")]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///etc/passwd")]
    [InlineData("mailto:alguem@exemplo.com")]
    [InlineData("www.exemplo.com/sem-esquema")]
    [InlineData("/caminho/relativo")]
    public async Task Rejects_other_schemes_and_relative_urls(string url)
    {
        var result = await Validator.ValidateAsync(url, CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Equal("A URL de destino deve ser absoluta e usar http ou https.", result.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Rejects_missing_url(string? url)
    {
        var result = await Validator.ValidateAsync(url, CancellationToken.None);

        Assert.Equal("A URL de destino é obrigatória.", result.Error);
    }

    [Fact]
    public async Task Accepts_url_with_exactly_2048_characters()
    {
        var result = await Validator.ValidateAsync(UrlWithLength(2048), CancellationToken.None);

        Assert.True(result.IsValid, result.Error);
    }

    [Fact]
    public async Task Rejects_url_longer_than_2048_characters()
    {
        var result = await Validator.ValidateAsync(UrlWithLength(2049), CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Equal("A URL de destino deve ter no máximo 2048 caracteres.", result.Error);
    }

    [Theory]
    [InlineData("10.1.2.3")]          // privada
    [InlineData("172.16.0.10")]       // privada
    [InlineData("192.168.1.1")]       // privada
    [InlineData("127.0.0.1")]         // loopback
    [InlineData("::1")]               // loopback IPv6
    [InlineData("169.254.169.254")]   // link-local
    [InlineData("fe80::1")]           // link-local IPv6
    [InlineData("fd00::1")]           // local único IPv6
    [InlineData("0.0.0.0")]           // reservada
    [InlineData("100.64.0.1")]        // reservada (CGNAT)
    [InlineData("192.0.2.10")]        // reservada (documentação)
    [InlineData("224.0.0.1")]         // multicast
    [InlineData("240.0.0.1")]         // reservada
    [InlineData("255.255.255.255")]   // broadcast
    [InlineData("2001:db8::1")]       // documentação IPv6
    [InlineData("::ffff:10.0.0.1")]   // IPv4 privado mapeado em IPv6
    public async Task Rejects_host_that_resolves_to_non_public_address(string address)
    {
        _resolver.Add("interno.exemplo.com", address);

        var result = await Validator.ValidateAsync("https://interno.exemplo.com/painel", CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Equal("O host 'interno.exemplo.com' da URL de destino precisa resolver para um endereço IP público.", result.Error);
    }

    [Fact]
    public async Task Rejects_host_when_any_resolved_address_is_not_public()
    {
        _resolver.Add("misto.exemplo.com", "93.184.215.14", "10.0.0.1");

        var result = await Validator.ValidateAsync("https://misto.exemplo.com", CancellationToken.None);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("http://127.0.0.1/admin")]
    [InlineData("http://192.168.0.1/")]
    [InlineData("http://[::1]:8080/")]
    public async Task Rejects_non_public_ip_literal_in_url(string url)
    {
        var result = await Validator.ValidateAsync(url, CancellationToken.None);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("http://93.184.215.14/")]
    [InlineData("http://[2606:2800:21f:cb07:6820:80da:af6b:8b2c]/")]
    public async Task Accepts_public_ip_literal_in_url(string url)
    {
        var result = await Validator.ValidateAsync(url, CancellationToken.None);

        Assert.True(result.IsValid, result.Error);
    }

    [Fact]
    public async Task Rejects_host_that_does_not_resolve_in_dns()
    {
        var result = await Validator.ValidateAsync("https://nao-existe.exemplo.com/", CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Equal("O host 'nao-existe.exemplo.com' da URL de destino não foi encontrado no DNS.", result.Error);
    }

    private static string UrlWithLength(int length)
    {
        const string prefix = "https://www.exemplo.com/";
        return prefix + new string('a', length - prefix.Length);
    }
}
