namespace LinkShortener.Api.Domain;

public sealed class Link
{
    public int Id { get; private set; }

    public string Code { get; private set; }

    public string TargetUrl { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Instante, em UTC, a partir do qual o link está expirado. Nulo quando o link não expira.
    /// </summary>
    public DateTime? ExpiresAtUtc { get; private set; }

    /// <summary>
    /// Só é alterado pelo incremento atômico do repositório (RN-04, D-02).
    /// </summary>
    public long AccessCount { get; private set; }

    public Link(string code, string targetUrl, DateTime createdAtUtc, DateTime? expiresAtUtc)
    {
        Code = code;
        TargetUrl = targetUrl;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }
}
