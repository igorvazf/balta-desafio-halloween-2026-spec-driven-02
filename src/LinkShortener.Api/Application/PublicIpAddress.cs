using System.Net;
using System.Net.Sockets;

namespace LinkShortener.Api.Application;

/// <summary>
/// Diz se um endereço IP é público, isto é, fora das faixas de loopback, rede privada,
/// link-local e reservadas (RN-02, CB-05).
/// </summary>
public static class PublicIpAddress
{
    private static readonly (IPAddress Network, int PrefixLength)[] NonPublicIPv4 =
    [
        (IPAddress.Parse("0.0.0.0"), 8),         // "esta rede"
        (IPAddress.Parse("10.0.0.0"), 8),        // privada
        (IPAddress.Parse("100.64.0.0"), 10),     // espaço compartilhado (CGNAT)
        (IPAddress.Parse("127.0.0.0"), 8),       // loopback
        (IPAddress.Parse("169.254.0.0"), 16),    // link-local
        (IPAddress.Parse("172.16.0.0"), 12),     // privada
        (IPAddress.Parse("192.0.0.0"), 24),      // atribuições de protocolo da IETF
        (IPAddress.Parse("192.0.2.0"), 24),      // documentação (TEST-NET-1)
        (IPAddress.Parse("192.88.99.0"), 24),    // relay 6to4, reservada
        (IPAddress.Parse("192.168.0.0"), 16),    // privada
        (IPAddress.Parse("198.18.0.0"), 15),     // testes de desempenho
        (IPAddress.Parse("198.51.100.0"), 24),   // documentação (TEST-NET-2)
        (IPAddress.Parse("203.0.113.0"), 24),    // documentação (TEST-NET-3)
        (IPAddress.Parse("224.0.0.0"), 4),       // multicast
        (IPAddress.Parse("240.0.0.0"), 4),       // reservada, inclui o broadcast
    ];

    private static readonly (IPAddress Network, int PrefixLength)[] NonPublicIPv6 =
    [
        (IPAddress.Parse("2001::"), 23),         // atribuições de protocolo da IETF
        (IPAddress.Parse("2001:db8::"), 32),     // documentação
    ];

    // Só o unicast global (2000::/3) é público; o resto do espaço IPv6 é loopback,
    // link-local, local único, multicast ou reservado.
    private static readonly (IPAddress Network, int PrefixLength) GlobalUnicastIPv6 = (IPAddress.Parse("2000::"), 3);

    public static bool IsPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        return address.AddressFamily switch
        {
            AddressFamily.InterNetwork => !NonPublicIPv4.Any(range => IsInRange(address, range)),
            AddressFamily.InterNetworkV6 => IsInRange(address, GlobalUnicastIPv6) && !NonPublicIPv6.Any(range => IsInRange(address, range)),
            _ => false,
        };
    }

    private static bool IsInRange(IPAddress address, (IPAddress Network, int PrefixLength) range)
    {
        var addressBytes = address.GetAddressBytes();
        var networkBytes = range.Network.GetAddressBytes();

        if (addressBytes.Length != networkBytes.Length)
        {
            return false;
        }

        var fullBytes = range.PrefixLength / 8;
        for (var i = 0; i < fullBytes; i++)
        {
            if (addressBytes[i] != networkBytes[i])
            {
                return false;
            }
        }

        var remainingBits = range.PrefixLength % 8;
        if (remainingBits == 0)
        {
            return true;
        }

        var mask = (byte)(0xFF << (8 - remainingBits));
        return (addressBytes[fullBytes] & mask) == (networkBytes[fullBytes] & mask);
    }
}
