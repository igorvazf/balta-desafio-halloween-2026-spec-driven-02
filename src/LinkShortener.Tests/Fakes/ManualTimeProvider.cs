namespace LinkShortener.Tests.Fakes;

/// <summary>
/// Relógio controlado pelo teste.
/// </summary>
public sealed class ManualTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = utcNow;

    public override DateTimeOffset GetUtcNow() => UtcNow;
}
