using System.Net;
using LinkShortener.Api.Application;

namespace LinkShortener.Tests.Fakes;

/// <summary>
/// DNS em memória: hosts não cadastrados não resolvem.
/// </summary>
public sealed class FakeHostAddressResolver : IHostAddressResolver
{
    public const string PublicHost = "www.exemplo.com";

    private readonly Dictionary<string, IPAddress[]> _hosts = new(StringComparer.OrdinalIgnoreCase)
    {
        [PublicHost] = [IPAddress.Parse("93.184.215.14")],
    };

    public FakeHostAddressResolver Add(string host, params string[] addresses)
    {
        _hosts[host] = addresses.Select(IPAddress.Parse).ToArray();
        return this;
    }

    public Task<IReadOnlyList<IPAddress>> ResolveAsync(string host, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<IPAddress>>(_hosts.TryGetValue(host, out var addresses) ? addresses : []);
}
