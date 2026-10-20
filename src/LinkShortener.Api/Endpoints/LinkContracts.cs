namespace LinkShortener.Api.Endpoints;

/// <param name="ExpiresAtUtc">Último dia de validade (aaaa-mm-dd), interpretado em UTC.</param>
public sealed record CreateLinkRequest(string? TargetUrl, string? Code, DateOnly? ExpiresAtUtc);

public sealed record CreateLinkResponse(string ShortUrl, string Code);

public sealed record LinkStatsResponse(string Code, long AccessCount);
