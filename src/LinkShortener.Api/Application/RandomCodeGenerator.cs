using System.Security.Cryptography;

namespace LinkShortener.Api.Application;

/// <summary>
/// Gera códigos de 7 caracteres com o alfabeto de <see cref="LinkCode"/> (RF-02).
/// </summary>
public sealed class RandomCodeGenerator : ICodeGenerator
{
    public string Generate() => RandomNumberGenerator.GetString(LinkCode.Alphabet, LinkCode.GeneratedLength);
}
