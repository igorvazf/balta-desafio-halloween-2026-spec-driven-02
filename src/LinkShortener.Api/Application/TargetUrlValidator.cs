using System.Net;

namespace LinkShortener.Api.Application;

public sealed record TargetUrlValidationResult(string? NormalizedUrl, string? Error)
{
    public bool IsValid => Error is null;

    public static TargetUrlValidationResult Valid(string normalizedUrl) => new(normalizedUrl, null);

    public static TargetUrlValidationResult Invalid(string error) => new(null, error);
}

/// <summary>
/// Valida a URL de destino (RN-02, CB-04, CB-05, CB-06). Não faz requisição ao destino.
/// </summary>
public sealed class TargetUrlValidator(IHostAddressResolver hostAddressResolver)
{
    public const int MaxLength = 2048;

    public async Task<TargetUrlValidationResult> ValidateAsync(string? targetUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(targetUrl))
        {
            return TargetUrlValidationResult.Invalid("A URL de destino é obrigatória.");
        }

        if (targetUrl.Length > MaxLength)
        {
            return TargetUrlValidationResult.Invalid($"A URL de destino deve ter no máximo {MaxLength} caracteres.");
        }

        if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || string.IsNullOrEmpty(uri.Host))
        {
            return TargetUrlValidationResult.Invalid("A URL de destino deve ser absoluta e usar http ou https.");
        }

        IReadOnlyList<IPAddress> addresses = IPAddress.TryParse(uri.IdnHost, out var literal)
            ? [literal]
            : await hostAddressResolver.ResolveAsync(uri.IdnHost, cancellationToken);

        if (addresses.Count == 0)
        {
            return TargetUrlValidationResult.Invalid($"O host '{uri.Host}' da URL de destino não foi encontrado no DNS.");
        }

        // Basta um endereço não público para recusar: o visitante poderia ser levado a ele.
        if (!addresses.All(PublicIpAddress.IsPublic))
        {
            return TargetUrlValidationResult.Invalid($"O host '{uri.Host}' da URL de destino precisa resolver para um endereço IP público.");
        }

        return TargetUrlValidationResult.Valid(uri.AbsoluteUri);
    }
}
