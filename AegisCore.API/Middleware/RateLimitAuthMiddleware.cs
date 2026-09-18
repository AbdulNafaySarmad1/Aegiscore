using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;

namespace AegisCore.API.Middleware;

/// <summary>
/// Slows down guessing at the sign-in endpoints, without slowing down a university.
///
/// This used to count every attempt from a client address and lock the address out for five
/// minutes past twenty a minute. A cohort arriving at nine o'clock shares one public address, so a
/// class signing in together locked itself out: thirty students signing in at once were refused
/// twelve times in a load test, and a real campus behind one NAT would fare worse.
///
/// Only failures are counted now. A successful sign-in is not an attack and costs nothing here;
/// the per-address ceiling for everything else is the global limiter's job, and guessing at one
/// person's password is the brute-force service's, which counts per account. The limits are
/// configurable because a campus of forty thousand is not a campus of four hundred.
/// </summary>
public class RateLimitAuthMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private static readonly ConcurrentDictionary<string, SlidingWindow> Windows = new();

    private readonly int _maxFailures = configuration.GetValue<int?>("RateLimit:SignInFailuresPerMinute") ?? 20;
    private readonly int _lockoutMinutes = configuration.GetValue<int?>("RateLimit:SignInLockoutMinutes") ?? 5;
    private const int WindowSeconds = 60;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/auth"))
        {
            await next(context);
            return;
        }

        var key = ClientAddress.Of(context);
        var window = Windows.GetOrAdd(key, _ => new SlidingWindow());
        var now = DateTime.UtcNow;

        lock (window)
        {
            window.Failures.RemoveAll(t => (now - t).TotalSeconds > WindowSeconds);
            if (window.LockedUntil > now)
            {
                context.Response.StatusCode = 429;
                context.Response.Headers["Retry-After"] = ((int)(window.LockedUntil - now).TotalSeconds).ToString();
                context.Response.ContentType = "application/json";
                context.Response.WriteAsJsonAsync(new
                {
                    error = $"Too many failed sign-in attempts from this connection. Try again in {Math.Max(1, (int)(window.LockedUntil - now).TotalMinutes)} minute(s).",
                    code = "sign_in_rate_limited",
                });
                return;
            }
        }

        await next(context);

        // What matters is how many attempts went wrong, which is knowable only now.
        if (context.Response.StatusCode is not (401 or 403)) return;

        lock (window)
        {
            window.Failures.Add(DateTime.UtcNow);
            if (window.Failures.Count > _maxFailures)
            {
                window.LockedUntil = DateTime.UtcNow.AddMinutes(_lockoutMinutes);
                window.Failures.Clear();
            }
        }
    }

    private sealed class SlidingWindow
    {
        public List<DateTime> Failures { get; } = [];
        public DateTime LockedUntil { get; set; } = DateTime.MinValue;
    }
}
