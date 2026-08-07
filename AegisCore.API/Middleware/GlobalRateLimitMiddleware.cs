using System.Collections.Concurrent;
using System.Security.Claims;

namespace AegisCore.API.Middleware;

public sealed class GlobalRateLimitMiddleware(RequestDelegate next)
{
    private static readonly ConcurrentDictionary<string, SlidingWindow> Windows = new();

    private const int PerIpMaxRequests = 100;
    private const int PerUserMaxRequests = 300;
    private const int WindowSeconds = 60;

    public async Task InvokeAsync(HttpContext context)
    {
        var keys = new List<string>();
        var ip = context.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(ip))
            keys.Add($"ip:{ip}");

        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
            keys.Add($"user:{userId}");

        foreach (var key in keys)
        {
            var limit = key.StartsWith("ip:") ? PerIpMaxRequests : PerUserMaxRequests;
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
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "rate_limit_exceeded",
                    message = "Too many requests. Please slow down."
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
