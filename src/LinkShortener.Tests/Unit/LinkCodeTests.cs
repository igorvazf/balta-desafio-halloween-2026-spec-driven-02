using LinkShortener.Api.Application;

namespace LinkShortener.Tests.Unit;

public sealed class LinkCodeTests
{
    [Theory]
    [InlineData("ABCD")]                    // 4 caracteres
    [InlineData("ABCDEFGHIJKLMNOPQRS9")]    // 20 caracteres
    [InlineData("PROMO26")]
    [InlineData("123456789")]
    public void Accepts_custom_code_with_4_to_20_valid_characters(string code)
    {
        Assert.Null(LinkCode.ValidateCustom(code));
    }

    [Theory]
    [InlineData("ABC")]                     // 3 caracteres
    [InlineData("ABCDEFGHIJKLMNOPQRS12")]   // 21 caracteres
    [InlineData("")]
    public void Rejects_custom_code_outside_4_to_20_characters(string code)
    {
        Assert.Equal("O código deve ter entre 4 e 20 caracteres.", LinkCode.ValidateCustom(code));
    }

    [Theory]
    [InlineData("promo")]                   // minúscula
    [InlineData("PROMo")]                   // uma minúscula
    [InlineData("PROMO2026")]               // dígito 0
    [InlineData("PRO MO")]                  // espaço
    [InlineData("PROMO-26")]                // caractere especial
    [InlineData("PROMO_26")]                // caractere especial
    [InlineData("PROMOÇÃO")]                // acento
    public void Rejects_custom_code_with_characters_outside_alphabet(string code)
    {
        Assert.Equal(
            "O código deve conter apenas letras maiúsculas de A a Z e dígitos de 1 a 9, sem espaços.",
            LinkCode.ValidateCustom(code));
    }
}
