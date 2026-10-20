using LinkShortener.Api.Domain;

namespace LinkShortener.Api.Application;

public interface ILinkRepository
{
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken);

    /// <summary>
    /// Grava o link. Devolve false, sem gravar nada, quando o código já existe no banco (D-04).
    /// </summary>
    Task<bool> TryAddAsync(Link link, CancellationToken cancellationToken);

    Task<Link?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    /// <summary>
    /// Soma um ao contador com um único UPDATE no banco (D-02).
    /// </summary>
    Task IncrementAccessCountAsync(int linkId, CancellationToken cancellationToken);
}
