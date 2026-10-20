using System.Net;
using System.Net.Sockets;

namespace LinkShortener.Api.Application;

public sealed class DnsHostAddressResolver : IHostAddressResolver
{
    public async Task<IReadOnlyList<IPAddress>> ResolveAsync(string host, CancellationToken cancellationToken)
    {
        try
        {
            return await Dns.GetHostAddressesAsync(host, cancellationToken);
        }
        catch (SocketException)
        {
            return [];
        }
    }
}
