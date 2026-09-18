using System.Net;

namespace AegisCore.API.Middleware;

/// <summary>
/// The address rate limits are keyed on. The portal proxy sits in the same container as the API,
/// so the API's remote address is 127.0.0.1 for every user; keying on it put the whole university
/// in one bucket (100 requests a minute, 10 sign-ins a minute). When the request arrives from the
/// loopback or a private network — the proxy, the ingress, a Docker bridge — the first
/// X-Forwarded-For address, which the portal proxy sets, is the client.
/// </summary>
public static class ClientAddress
{
    public static string Of(HttpContext context)
    {
        var remote = context.Connection.RemoteIpAddress;
        if (remote is not null && (IPAddress.IsLoopback(remote) || IsPrivate(remote)))
        {
            var forwarded = context.Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrWhiteSpace(forwarded))
            {
                var first = forwarded.Split(',')[0].Trim();
                if (IPAddress.TryParse(first, out var parsed)) return parsed.ToString();
                if (first.Length is > 0 and <= 64) return first; // a proxy-assigned label such as "portal"
            }
        }
        return remote?.ToString() ?? "unknown";
    }

    private static bool IsPrivate(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        var b = address.GetAddressBytes();
        if (b.Length != 4) return address.IsIPv6LinkLocal || address.IsIPv6UniqueLocal;
        return b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168);
    }
}
