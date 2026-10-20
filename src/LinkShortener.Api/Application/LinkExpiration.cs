namespace LinkShortener.Api.Application;

/// <summary>
/// Regras da data de expiração (RF-06, CB-07, RN-03). A data é um dia, sem horário, em UTC.
/// </summary>
public static class LinkExpiration
{
    /// <summary>
    /// A data atual é aceita; só datas anteriores a hoje, em UTC, são recusadas.
    /// </summary>
    public static bool IsInThePast(DateOnly expirationDate, DateTimeOffset now) =>
        expirationDate < DateOnly.FromDateTime(now.UtcDateTime);

    /// <summary>
    /// O link vale até 23:59:59 UTC da data e expira a partir de 00:00 UTC do dia seguinte.
    /// </summary>
    public static DateTime ExpiresAtUtc(DateOnly expirationDate) =>
        expirationDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    /// <param name="expiresAtUtc">Instante de expiração gravado no link; nulo quando o link não expira.</param>
    public static bool IsExpired(DateTime? expiresAtUtc, DateTimeOffset now) =>
        expiresAtUtc is { } expiresAt && now.UtcDateTime >= expiresAt;
}
