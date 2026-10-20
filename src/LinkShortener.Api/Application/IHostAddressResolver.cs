using System.Net;

namespace LinkShortener.Api.Application;

public interface IHostAddressResolver
{
    /// <summary>
    /// Devolve os endereços IP do host, ou uma lista vazia quando o host não resolve no DNS.
    /// </summary>
    Task<IReadOnlyList<IPAddress>> ResolveAsync(string host, CancellationToken cancellationToken);
}
