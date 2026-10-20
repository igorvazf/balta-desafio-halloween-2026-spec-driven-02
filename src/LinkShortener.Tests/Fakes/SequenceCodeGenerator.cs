using LinkShortener.Api.Application;

namespace LinkShortener.Tests.Fakes;

/// <summary>
/// Gerador que devolve os códigos informados, em ordem; o último se repete quando a lista acaba.
/// </summary>
public sealed class SequenceCodeGenerator(params string[] codes) : ICodeGenerator
{
    public int Calls { get; private set; }

    public string Generate() => codes[Math.Min(Calls++, codes.Length - 1)];
}
