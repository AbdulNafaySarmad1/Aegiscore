using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace AegisCore.API.Middleware;

/// <summary>
/// <c>/metrics</c> was anonymous to the whole internet and carried a per-tenant label, which told
/// anyone who asked which tenants exist and how much traffic each one takes. It is now reachable
/// only by a scraper that presents the configured token, by a signed-in user, or — for the common
/// case of Prometheus running beside the application — from a private network address.
/// Compliance control AC-33.
/// </summary>
public sealed class MetricsAccessMiddleware(RequestDelegate next)
{
    public const string TokenHeader = "X-Metrics-Token";

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        if (!context.Request.Path.StartsWithSegments("/metrics", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var configured = configuration["Metrics:ScrapeToken"] ?? configuration["METRICS_SCRAPE_TOKEN"];
        if (!string.IsNullOrWhiteSpace(configured) && Presented(context) is { } presented
            && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(presented), Encoding.UTF8.GetBytes(configured)))
        {
            await next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated == true)
        {
            await next(context);
            return;
        }

        // A scraper on the same host or private network, which is how the compose and Kubernetes
        // deployments run it. Turn this off with Metrics:AllowPrivateNetwork=false once a token is set.
        if (configuration.GetValue("Metrics:AllowPrivateNetwork", true) && IsPrivate(context.Connection.RemoteIpAddress))
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status404NotFound;
    }

    private static string? Presented(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(TokenHeader, out var header) && !string.IsNullOrEmpty(header)) return header.ToString();
        var authorization = context.Request.Headers.Authorization.ToString();
        return authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? authorization[7..] : null;
    }

    private static bool IsPrivate(IPAddress? address)
    {
        if (address is null) return false;
        if (IPAddress.IsLoopback(address)) return true;
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var octets = address.GetAddressBytes();
            return octets[0] switch
            {
                10 => true,
                127 => true,
                172 => octets[1] >= 16 && octets[1] <= 31,
                192 => octets[1] == 168,
                169 => octets[1] == 254,
                _ => false,
            };
        }
        return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal
            || (address.GetAddressBytes()[0] & 0xfe) == 0xfc; // unique local fc00::/7
    }
}
