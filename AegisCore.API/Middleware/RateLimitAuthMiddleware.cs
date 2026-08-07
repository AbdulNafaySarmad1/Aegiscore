using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace AegisCore.API.Middleware;

/// <summary>
/// In-memory rate limiter for authentication endpoints.
/// Uses sliding window: max 10 requests per IP per minute on /api/auth/*
/// </summary>
public class RateLimitAuthMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ConcurrentDictionary<string, SlidingWindow> _windows = new();

    private const int MaxRequests = 10;
    private const int WindowSeconds = 60;
    private const int LockoutMinutes = 5;

    public RateLimitAuthMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/auth"))
        {
            await _next(context);
            return;
        }

        var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var window = _windows.GetOrAdd(key, _ => new SlidingWindow());

        lock (window)
        {
            var now = DateTime.UtcNow;
            window.Timestamps.RemoveAll(t => (now - t).TotalSeconds > WindowSeconds);
            window.Timestamps.Add(now);

            if (window.LockedUntil > now)
            {
                context.Response.StatusCode = 429;
                context.Response.ContentType = "application/json";
                context.Response.WriteAsJsonAsync(new { error = "Too many requests. Please wait before trying again." });
                return;
            }

            if (window.Timestamps.Count > MaxRequests)
            {
                window.LockedUntil = now.AddMinutes(LockoutMinutes);
                context.Response.StatusCode = 429;
                context.Response.ContentType = "application/json";
                context.Response.WriteAsJsonAsync(new { error = "Rate limit exceeded. Your IP has been temporarily blocked from authentication." });
                return;
            }
        }

        await _next(context);
    }

    private class SlidingWindow
    {
        public List<DateTime> Timestamps { get; } = new();
        public DateTime LockedUntil { get; set; } = DateTime.MinValue;
    }
}
