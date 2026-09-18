using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;

namespace AegisCore.API.Middleware;

/// <summary>
/// A sliding-window limit per client address and per signed-in user.
///
/// This used to key on <c>Connection.RemoteIpAddress</c>, which behind the portal proxy is the
/// proxy itself: the whole university shared one bucket of 100 requests a minute, and ordinary
/// browsing — eleven screens in a row — exhausted it and answered everyone with 429. It keys on
/// the forwarded client address now, like the sign-in limiter, and both limits are configurable
/// because an institution with a busy front desk is not an institution under attack.
/// </summary>
public sealed class GlobalRateLimitMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private static readonly ConcurrentDictionary<string, SlidingWindow> Windows = new();

    private readonly int _perAddress = configuration.GetValue<int?>("RateLimit:PerAddressPerMinute") ?? 600;
    private readonly int _perUser = configuration.GetValue<int?>("RateLimit:PerUserPerMinute") ?? 600;
    private const int WindowSeconds = 60;

    public async Task InvokeAsync(HttpContext context)
    {
        var keys = new List<(string Key, int Limit)>();

        var address = ClientAddress.Of(context);
        if (!string.IsNullOrEmpty(address)) keys.Add(($"ip:{address}", _perAddress));

        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId)) keys.Add(($"user:{userId}", _perUser));

        foreach (var (key, limit) in keys)
        {
            var window = Windows.GetOrAdd(key, _ => new SlidingWindow());
            var now = DateTimeOffset.UtcNow;

            bool rateLimited;
            lock (window)
            {
                window.Timestamps.RemoveAll(t => (now - t).TotalSeconds > WindowSeconds);
                window.Timestamps.Add(now);
                rateLimited = window.Timestamps.Count > limit;
            }

            if (rateLimited)
            {
                context.Response.StatusCode = 429;
                context.Response.Headers["Retry-After"] = WindowSeconds.ToString();
                context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
                context.Response.Headers["X-RateLimit-Reset"] = now.AddSeconds(WindowSeconds).ToUnixTimeSeconds().ToString();
                context.Response.ContentType = "application/json";
                // `error` is the sentence a person reads; the portal shows it verbatim.
                await context.Response.WriteAsJsonAsync(new
                {
                    error = $"This connection has made more than {limit} requests in a minute, so the register is pausing it. Wait a minute and try again; if this happens during normal use, raise RateLimit__PerAddressPerMinute.",
                    code = "rate_limit_exceeded",
                    message = $"More than {limit} requests in a minute from this connection."
                }, context.RequestAborted);
                return;
            }
        }

        await next(context);
    }

    private sealed class SlidingWindow
    {
        public List<DateTimeOffset> Timestamps { get; } = new();
    }
}
