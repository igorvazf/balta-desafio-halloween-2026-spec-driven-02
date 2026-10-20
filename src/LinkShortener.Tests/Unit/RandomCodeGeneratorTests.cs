using LinkShortener.Api.Application;

namespace LinkShortener.Tests.Unit;

public sealed class RandomCodeGeneratorTests
{
    private const int Samples = 10_000;

    private readonly RandomCodeGenerator _generator = new();

    [Fact]
    public void Generated_code_has_seven_characters()
    {
        for (var i = 0; i < Samples; i++)
        {
            Assert.Equal(7, _generator.Generate().Length);
        }
    }

    [Fact]
    public void Generated_code_uses_only_uppercase_letters_and_digits_one_to_nine()
    {
        for (var i = 0; i < Samples; i++)
        {
            var code = _generator.Generate();

            Assert.All(code, c => Assert.True(c is (>= 'A' and <= 'Z') or (>= '1' and <= '9'), $"Caractere inválido '{c}' em {code}"));
        }
    }

    [Fact]
    public void Generated_code_never_contains_digit_zero()
    {
        for (var i = 0; i < Samples; i++)
        {
            Assert.DoesNotContain('0', _generator.Generate());
        }
    }

    [Fact]
    public void Alphabet_is_exactly_A_to_Z_and_1_to_9()
    {
        var expected = string.Concat(Enumerable.Range('A', 26).Select(c => (char)c)) + "123456789";

        Assert.Equal(expected.ToCharArray(), LinkCode.Alphabet.ToCharArray());
    }
}
